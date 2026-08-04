# Deploying Goose on Debian 12 with Docker

This guide deploys the server on a fresh Debian 12 VPS using Docker. The image is
read-only; all mutable state — the SQLite database, `Logs/`, and your
`GooseSettings.json` — lives on a persistent Docker volume mounted at `/data`
(the server's `--datadir`).

## 1. Install Docker

```sh
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Add your user to the `docker` group (then log out and back in, or use `sudo` for
the remaining commands):

```sh
sudo usermod -aG docker $USER
```

## 2. Get the code

```sh
git clone git@github.com:okonk/gooseserver.git
cd gooseserver
```

## 3. Configure before the first run

Edit `GooseSettings.json` and make sure `ServerType`, `DatabaseName`, `DataLinkId`
and `DataPath` match the game you're running. The file in the repo is currently
inconsistent (`ServerType: Illutia` but `DatabaseName: AsperetaGoose` with the
Aspereta sheet ID) — fix it now, because the first start imports whatever sheet is
configured into the database.

The first start also seeds a copy of this file into the data volume
(`/data/GooseSettings.json`). From then on that copy is authoritative — to change
settings later, edit it (see step 7) rather than the repo copy.

## 4. Build and start

```sh
docker compose up -d --build
```

First build downloads the .NET 10 SDK/runtime images and NuGet packages, so give
it a few minutes. Watch the first start:

```sh
docker compose logs -f goose
```

You should see it create the schema, import the Google Sheet, load the game, and
finish with `Finished loading game. Ready to join.` The server listens on TCP
**2006**.

## 5. Firewall

Open the game port (adjust to your provider's firewall if you use one):

```sh
sudo ufw allow 2006/tcp
```

Configure your client to connect to the VPS IP on port 2006.

## 6. Making a character a GM

The image ships `sqlite3`, so from the host:

```sh
docker compose exec goose sqlite3 /data/AsperetaGoose.db \
  "UPDATE players SET access_status=9 WHERE player_name='namegoeshere';"
```

(The DB filename comes from `DatabaseName` in `GooseSettings.json`.)

## 7. Editing settings / data dir layout

Everything persistent lives in the `gooseserver_goose-data` volume:

| Path in container | Purpose |
|---|---|
| `/data/GooseSettings.json` | settings (seeded from the image on first start) |
| `/data/<DatabaseName>.db` (+ `-wal`/`-shm`) | the game database |
| `/data/Logs/` | server logs |
| `/data/GooseData.sql` | last `updatesql` export (when run) |
| `/data/crashlog.txt` | last crash stack trace (when present) |

To inspect or edit the volume from the host:

```sh
docker run --rm -it -v gooseserver_goose-data:/data alpine sh
```

To pull a file out:

```sh
docker run --rm -v gooseserver_goose-data:/data -v "$PWD":/out alpine \
  cp /data/GooseSettings.json /out/
```

## 8. Updating the server

```sh
git pull
docker compose up -d --build
```

The volume is untouched, so players, settings, and logs survive. To re-import game
data from the sheet, stop the server first (the running server holds the DB
exclusively), then run `updatesql` — it imports and exits on its own — and start
again:

```sh
docker compose stop
docker compose run --rm goose dotnet Goose.dll updatesql
docker compose start
```

(or use the in-game GM command `/updatesql` for a live update).

## 9. Backups

Stop the container first — SQLite runs in WAL mode and copying only the `.db`
while `-wal` has uncheckpointed data produces an incomplete backup:

```sh
docker compose stop
docker run --rm -v gooseserver_goose-data:/data -v "$PWD":/backup alpine \
  tar czf /backup/goose-backup-$(date +%F).tgz -C /data .
docker compose start
```

## 10. Custom scripts

By default `Data/` (maps, `.csx` scripts) ships in the image and updates with
`git pull` + rebuild. If you want operator-owned scripts that survive rebuilds,
copy the scripts out once and bind-mount them (see the commented volume in
`docker-compose.yml`):

```sh
mkdir -p server-scripts
docker compose cp goose:/app/Data/Illutia/Scripts/. server-scripts/
```

then uncomment the `./server-scripts:/app/Data/Illutia/Scripts` volume in
`docker-compose.yml` and `docker compose up -d`. Note this replaces the image's
scripts wholesale (including `CrystalCritterSpawner.csx`).

## Troubleshooting

* **Import fails on first start** — the container needs outbound HTTPS to
  Google Sheets; check `DataLinkId` and that the sheet is shared publicly.
* **Port not reachable** — the server binds `0.0.0.0:2006`; check the host
  firewall (`ufw status`) and your provider's security group.
* **Container exits immediately** — `docker compose logs goose` shows the
  reason; a bad `GooseSettings.json` aborts at startup.
