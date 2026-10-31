#!/bin/sh
# Optional only: existing SteamOS uaccess permissions already work on this Deck.
set -eu
if [ "$(id -u)" -ne 0 ]; then exec sudo sh "$0"; fi
cat > /etc/udev/rules.d/70-steam-controller-viewer.rules <<'RULES'
SUBSYSTEM=="hidraw", KERNEL=="hidraw*", ATTRS{idVendor}=="28de", ATTRS{idProduct}=="1304", TAG+="uaccess"
RULES
udevadm control --reload-rules
udevadm trigger --subsystem-match=hidraw --action=change
printf '%s\n' 'Receiver 28de:1304 uaccess rule installed. Reconnect the receiver if access is not refreshed.'
