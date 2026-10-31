# Steam Controller Viewer — Linux v2.0.2

Extract the release and run `./launch.sh`, or `./SteamControllerGamepadViewer`.
Steam can remain running. The .NET runtime is included; no SDK is required.
Keep the extracted directory in a stable location.

OBS Browser Source: **http://127.0.0.1:31337/**, **1200 × 900**, Custom CSS empty.
Diagnostics: http://127.0.0.1:31337/diagnostics.html
Debug: `./launch.sh --debug`.

## Press Colors

Open http://127.0.0.1:31337/settings.html for one global highlight color or
individual colors per control. Save updates open overlays automatically.
Default and Reset use the original blue (#25a7ff). Settings persist in
`~/.config/SteamControllerGamepadViewer/press-colors.json` (or `$XDG_CONFIG_HOME`).

## Desktop launcher and optional autostart

Run `python3 install-desktop.py` to install the application-menu launcher and
user service for this extracted directory. This does not enable autostart.
Use `systemctl --user start steam-controller-viewer.service` to start it and
`systemctl --user stop steam-controller-viewer.service` to stop it.
Logs: `journalctl --user -u steam-controller-viewer.service -f`.
Enable autostart: `systemctl --user enable steam-controller-viewer.service`.
Disable: `systemctl --user disable steam-controller-viewer.service`.
Stop the service before running debug mode directly.

## HID permissions

If device access is denied, run `./install-udev.sh` (requires sudo only for the rule).
It grants session access solely to Valve receiver 28de:1304. Reconnect if needed.
Other hardware IDs need their own reviewed rule. Never run the viewer as root.

## Limitations and credits

BLE framing is not implemented. View/Menu/Steam transitions and digital pad-click
flags are not conclusively hardware verified. Classic hardware and physical
sleep/unplug/reconnect are not fully verified. See the repository support table.

Linux/SteamOS port of Ramonchi_5’s Steam Controller Gamepad Viewer:
https://github.com/ramonchi5/Steam-Controller-Gamepad-Viewer-by-Ramonchi_5
Code: MIT; retain LICENSE and THIRD_PARTY_NOTICES.md. Valve names/artwork remain
Valve property. Unofficial project; no Valve affiliation, endorsement or sponsorship.
