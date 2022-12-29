# Usage

Install the .deb from actions `dpkg -i something.deb`.

This installs a service `elitesx2mqtt.service` that can be controlled using `systemctl`. Logs go to `journalctl`

## Configuration

```sh
sudo -s
cd /opt/bin/elitesx2mqtt/
cp appsettings.Local.json.TEMPLATE appsettings.Local.json
nano appsettings.Local.json

... populate it ...

sudo systemctl restart elitesx2mqtt.service
journalctl -u elitesx2mqtt.service -f
```

If everything is configured correctly it'll appear in home assistant automatically.