# Steam Controller Viewer — Linux / SteamOS

Launch **Steam Controller Viewer** from the Desktop Mode application menu, or run
`/home/deck/SteamControllerViewerLinux/launch.sh`. Steam can remain running.

OBS: Browser Source, URL **http://127.0.0.1:31337/**, width **1200**, height **900**,
Custom CSS empty. Existing visual query parameters remain supported.
Live diagnostic screen: http://127.0.0.1:31337/diagnostics.html

Installed release: `/home/deck/SteamControllerViewerLinux/artifacts/linux-x64/`.
Self-contained Linux x64 executable: `SteamControllerGamepadViewer`.
Source and project-local SDK: `/home/deck/SteamControllerViewerLinux/`.
Baseline: upstream 0a9d09c, local branch `linux-port`.

The launcher starts the user service once; repeated launches do not create competing servers.
Stop: `systemctl --user stop steam-controller-viewer.service`.
Debug: stop the service, then run `./launch.sh --debug` from this release folder.
Logs: `journalctl --user -u steam-controller-viewer.service -f`.

Optional autostart (installed but NOT enabled):
`systemctl --user enable steam-controller-viewer.service`
Disable: `systemctl --user disable steam-controller-viewer.service`.

HID permissions already work on this Deck. Only if access is denied, run
`./install-udev.sh` (asks for sudo). It grants session access only to the observed
Valve receiver 28de:1304; no root viewer, firmware commands, device grabs, or remapping.

The Linux backend reads physical vendor HID reports directly and does not initialize
SDL's hardware drivers, avoiding controller configuration writes. The Deck's internal
controller is deliberately excluded so it cannot replace a sleeping external controller.
Triton 0x42/0x45 packets retain upstream decoding. Classic 2015 USB/dongle reports
also decode controls and multiplexed pad/stick coordinates; classic hardware was not
attached for physical verification. BLE framing is not implemented. A classic 2015
controller has two rear levers, not four; unavailable touch/pressure sensors are not invented.
The original upstream controller art is retained. Only one active external controller
is displayed. On loss of reports the overlay clears after 1.5 seconds; rediscovery runs
every second. Physical sleep/reconnect has not been hardware-verified.

For another installation path, edit the supplied user service's ExecStart and
WorkingDirectory before copying it to `~/.config/systemd/user/`, then run
`systemctl --user daemon-reload`. The portable `./launch.sh --debug` runs directly.
