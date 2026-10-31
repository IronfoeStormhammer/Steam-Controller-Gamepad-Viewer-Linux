The first polished public Linux/SteamOS release of **Ramonchi_5’s Steam Controller Gamepad Viewer port**.

- Physical Steam Controller HID input while **Steam remains running**.
- Live left/right trackpads, pressure and touch; L4/L5/R4/R5; grip sensing;
  analog triggers; joystick movement/click/capacitive touch; D-pad.
- OBS Browser Source, live diagnostics and optional Linux desktop integration.
- **Global or per-control pressed colors** at http://127.0.0.1:31337/settings.html.
  The original blue remains the default; saved settings persist across restarts
  and update open overlays without restarting OBS.
- **38 passing tests**. Self-contained Linux x64 runtime; no development SDK needed.

### Quick start

Download the tar.gz and SHA256SUMS, verify with `sha256sum -c SHA256SUMS`, extract,
and run `./launch.sh`. Install the narrow udev helper only if HID access is denied.

OBS Browser Source: **http://127.0.0.1:31337/** · **1200 × 900** · Custom CSS empty.
Diagnostics: http://127.0.0.1:31337/diagnostics.html

### Known limitations

Hardware verification used Valve receiver **28de:1304** alongside Steam.
View/Menu/Steam transitions and digital trackpad-click flags were not conclusively
captured. Classic-controller hardware and physical reconnect are not fully verified.
BLE framing is not implemented. Sensor availability varies by hardware generation.

### Credits

Original project: [Ramonchi_5](https://github.com/ramonchi5/Steam-Controller-Gamepad-Viewer-by-Ramonchi_5).
MIT code licensing and upstream attribution are retained. Valve artwork/trademarks
remain Valve property. This unofficial project is not endorsed or sponsored by Valve.
