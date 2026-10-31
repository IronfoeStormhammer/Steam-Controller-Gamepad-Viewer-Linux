# Changelog

## v2.0.2

First polished public Linux x86_64 / SteamOS release of the browser-source port.

- Passive physical Steam Controller HID input while Steam remains running.
- Live trackpad positions, touch and pressure; L4/L5/R4/R5; grip-sense regions.
- Analog triggers, joystick movement/click/capacitive touch and D-pad visualization.
- OBS Browser Source at localhost:31337 with existing upstream artwork and styles.
- **Persistent global/per-control press colors**, color-picker settings page,
  live updates, and reset to the original blue.
- Linux launcher, optional desktop integration/user service, narrow udev helper,
  live diagnostics, automatic rediscovery and stale-state clearing.
- 38 passing tests, Linux build/release workflow, redistributable runtime archive.

Hardware verification used receiver 28de:1304 alongside Steam. View/Menu/Steam
transitions and digital trackpad-click flags remain inconclusive. Classic hardware
and physical reconnect are not fully verified; BLE framing is not implemented.

Derived from Ramonchi_5’s MIT-licensed Steam Controller Gamepad Viewer; upstream
history and copyright notices are retained.
