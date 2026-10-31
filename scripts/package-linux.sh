#!/usr/bin/env bash
set -euo pipefail
ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
VERSION=${1:-2.0.2}
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo 'Expected numeric x.y.z version' >&2; exit 1; }
DOTNET=${DOTNET:-dotnet}
mkdir -p "$ROOT/artifacts/release"
TEMP=$(mktemp -d "$ROOT/artifacts/package.XXXXXX")
trap 'rm -rf -- "$TEMP"' EXIT
if [[ $# -ge 2 ]]; then
  PUBLISH=$(realpath -- "$2")
else
  PUBLISH="$TEMP/publish"
  "$DOTNET" publish "$ROOT/src/SteamControllerGamepadViewer/SteamControllerGamepadViewer.csproj" -c Release -r linux-x64 --self-contained true -o "$PUBLISH"
fi
APP="$TEMP/SteamControllerViewer-linux-x64"
mkdir -p "$APP"
cp -a "$PUBLISH/." "$APP/"
# Packages are made from a fresh dotnet publish directory, never an installed profile.
rm -f "$APP"/*.pdb "$APP/README.md"
cp "$ROOT/README-Linux.md" "$APP/README.md"
cp "$ROOT/LICENSE" "$ROOT/THIRD_PARTY_NOTICES.md" "$APP/"
cp "$ROOT/release-assets/linux/launch.sh" "$ROOT/release-assets/linux/install-udev.sh" "$ROOT/release-assets/linux/install-desktop.py" "$APP/"
SDKROOT=$(dirname -- "$(realpath -- "$(command -v "$DOTNET")")")
for NOTICE in LICENSE.txt ThirdPartyNotices.txt; do
  [[ -f "$SDKROOT/$NOTICE" ]] || { echo "Missing SDK notice $SDKROOT/$NOTICE" >&2; exit 1; }
  cp "$SDKROOT/$NOTICE" "$APP/DOTNET_$NOTICE"
done
printf '%s\n' "v$VERSION Linux x64" "Source: $(git -C "$ROOT" rev-parse HEAD)" > "$APP/RELEASE-VERSION.txt"
ARCHIVE="Steam.Controller.Gamepad.Viewer-Linux-x64-v$VERSION.tar.gz"
tar -czf "$ROOT/artifacts/release/$ARCHIVE" -C "$TEMP" SteamControllerViewer-linux-x64
(cd "$ROOT/artifacts/release" && sha256sum "$ARCHIVE" > SHA256SUMS && sha256sum -c SHA256SUMS)
printf '%s\n' "$ROOT/artifacts/release/$ARCHIVE"
