#!/bin/bash
# start the game on this Mac (Steam build, BepInEx under Rosetta) in an isolated profile .harness/profile: the game
# folder's own BepInEx/plugins are never loaded or touched. Fresh DLLs are copied first; the harness loads
# CLIENTPORTALS_WORLD (default PortalShowcase) as CLIENTPORTALS_CHAR, in a window of CLIENTPORTALS_SCREEN_W x _H (1280x720). Refuses (exit 3) while any Valheim runs here.

R=$(cd "$(dirname "$0")/../.." && pwd)
G="${VALHEIM_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Valheim}"
P="$R/.harness/profile/BepInEx"
pgrep -f "MacOS/Valheim( |$)" >/dev/null && { echo "BUSY: Valheim is already running on this Mac: not starting another" >&2; exit 3; }
mkdir -p "$P/core" "$P/config" "$P/plugins/ClientPortals" "$P/plugins/ClientPortalsDevHarness" "$R/.harness/shots"
rsync -a "$G/BepInEx/core/" "$P/core/"
[ -f "$P/config/BepInEx.cfg" ] || cp "$G/BepInEx/config/BepInEx.cfg" "$P/config/"
cp "$R/ClientPortals/bin/Release/ClientPortals.dll" "$P/plugins/ClientPortals/"
cp "$R/tools/DevHarness/bin/Release/ClientPortalsDevHarness.dll" "$P/plugins/ClientPortalsDevHarness/"
# a command the last game never read must not reach this one
rm -f "$R/.harness/cmd.txt"
cd "$G"
CLIENTPORTALS_HARNESS_DIR="$R/.harness" CLIENTPORTALS_AUTOSTART=1 CLIENTPORTALS_WORLD="${CLIENTPORTALS_WORLD:-PortalShowcase}" \
  nohup /usr/bin/arch -x86_64 /bin/bash ./start_game_bepinex.sh --doorstop-target-assembly "$P/core/BepInEx.Preloader.dll" \
  -screen-fullscreen 0 -screen-width "${CLIENTPORTALS_SCREEN_W:-1280}" -screen-height "${CLIENTPORTALS_SCREEN_H:-720}" > "$R/.harness/game.out" 2>&1 &
n=$( [ -f "$R/.harness/log.txt" ] && wc -l < "$R/.harness/log.txt" || echo 0 )
for i in $(seq 1 60); do
  sleep 3
  out=$(tail -n +$((n + 1)) "$R/.harness/log.txt" 2>/dev/null)
  echo "$out" | grep -q "starting world\|! " && { echo "$out"; exit 0; }
  pgrep -f "MacOS/Valheim( |$)" >/dev/null || { echo "the game exited"; tail -20 "$R/.harness/game.out"; exit 1; }
done
echo "TIMEOUT waiting for the world"; exit 1
