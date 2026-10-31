# Contributing

Use a .NET 8 SDK and the commands in README’s Building From Source section.
Run `dotnet restore SteamControllerGamepadViewer.sln`,
`dotnet build SteamControllerGamepadViewer.sln -c Release --no-restore`, and
`dotnet test SteamControllerGamepadViewer.sln -c Release --no-build` before submitting.
Package with `./scripts/package-linux.sh 2.0.2`.

Keep changes focused; preserve the passive Linux reader, Steam coexistence,
Windows support, existing overlay geometry and original default appearance.
Add focused regression tests for report decoding, settings or behavior changes.
Distinguish synthetic/parser tests from physical hardware verification.
Preserve upstream attribution, MIT notices and third-party notices.

For bugs, include application version, distribution/kernel, connection mode,
Steam running yes/no, reproduction steps and relevant short debug output.
Redact account names, serial numbers and personal paths. Do not submit tokens,
Steam account data, OBS profiles, raw environment dumps or large packet captures.
Use the controller-detection template for input discovery problems.
