#!/usr/bin/env python3
"""Optional per-user desktop integration; never enables autostart."""
import json, os, pathlib, subprocess
root = pathlib.Path(__file__).resolve().parent
config = pathlib.Path(os.environ.get("XDG_CONFIG_HOME", str(pathlib.Path.home()/".config")))
data = pathlib.Path(os.environ.get("XDG_DATA_HOME", str(pathlib.Path.home()/".local/share")))
unit = config/"systemd/user/steam-controller-viewer.service"
unit.parent.mkdir(parents=True, exist_ok=True)
def quote(path): return json.dumps(str(path), ensure_ascii=False).replace("%", "%%")
unit.write_text("[Unit]\nDescription=Steam Controller Viewer\nAfter=graphical-session.target\nPartOf=graphical-session.target\n\n[Service]\nType=simple\nExecStart="+quote(root/"SteamControllerGamepadViewer")+"\nWorkingDirectory="+quote(root)+"\nRestart=on-failure\nRestartSec=2\nTimeoutStopSec=5\n\n[Install]\nWantedBy=default.target\n")
entry = data/"applications/Steam-Controller-Viewer.desktop"
entry.parent.mkdir(parents=True, exist_ok=True)
entry.write_text("[Desktop Entry]\nType=Application\nName=Steam Controller Viewer\nComment=Local Steam Controller overlay for OBS\nExec=systemctl --user start steam-controller-viewer.service\nIcon=input-gaming\nTerminal=false\nCategories=Game;Utility;\n")
subprocess.run(["systemctl","--user","daemon-reload"],check=True)
print("Desktop launcher installed. Autostart remains disabled unless you enable it explicitly.")
