#!/bin/bash
# stop this profile's game on the Windows test machine, copy fresh DLLs, start it again (scheduled task, interactive session) and wait for the world.
# Refuses (exit 3) while any other Valheim runs there.
# CLIENTPORTALS_TASK=ClientPortalsShowcase loads the showcase world (portals placed and named for the mod page) instead
W=$(dirname "$0")/remote-ps.sh
: "${CLIENTPORTALS_REMOTE:?set CLIENTPORTALS_REMOTE=user@host of the Windows test machine (e.g. in mise.local.toml)}"
R=$(cd "$(dirname "$0")/../.." && pwd)
# never touch a game this profile didn't start (the user playing, another test profile)
other=$($W <<'EOS' | tr -d '\r'
$p = Get-CimInstance Win32_Process -Filter "Name='valheim.exe'"
if (@($p | ? { $_.CommandLine -notlike '*ClientPortalsTest*' }).Count -gt 0) { 'OTHER' }
EOS
)
[ "$other" = OTHER ] && { echo "BUSY: another Valheim is running on the test machine (not this profile's): not stopping it" >&2; exit 3; }
$W <<'EOS'
if (Get-Process valheim -ErrorAction SilentlyContinue) {
  Set-Content -Path D:\ClientPortalsTest\harness\cmd.txt -Value 'quit'
  for ($i = 0; $i -lt 60 -and (Get-Process valheim -ErrorAction SilentlyContinue); $i++) { Start-Sleep 1 }
  $ours = Get-CimInstance Win32_Process -Filter "Name='valheim.exe'" | ? { $_.CommandLine -like '*ClientPortalsTest*' }
  if ($ours) { $ours | % { Stop-Process -Id $_.ProcessId -Force }; 'killed' } else { 'quit cleanly' }
} else { 'not running' }
# a command the old game never read (it hung) must not reach the new one
Remove-Item D:\ClientPortalsTest\harness\cmd.txt -ErrorAction SilentlyContinue
Start-Sleep 2
EOS
# a game still running has the DLLs loaded: overwriting them corrupts it, and a second game would share cmd.txt
left=$($W <<< "@(Get-Process valheim -ErrorAction SilentlyContinue).Count" | tr -d '\r')
[ "$left" = 0 ] || { echo "the test game didn't stop ($left running): not copying or starting" >&2; exit 4; }
scp -q -o LogLevel=ERROR "$R/ClientPortals/bin/Release/ClientPortals.dll" "$CLIENTPORTALS_REMOTE:D:/ClientPortalsTest/BepInEx/plugins/ClientPortals/"
scp -q -o LogLevel=ERROR "$R/tools/DevHarness/bin/Release/ClientPortalsDevHarness.dll" "$CLIENTPORTALS_REMOTE:D:/ClientPortalsTest/BepInEx/plugins/ClientPortalsDevHarness/"
# only lines the new game writes count (an old "joining" line would end the wait at once)
n=$($W <<< "if (Test-Path D:\ClientPortalsTest\harness\log.txt) { (Get-Content D:\ClientPortalsTest\harness\log.txt).Count } else { 0 }" | tr -d '\r')
$W <<< "schtasks /Run /TN ${CLIENTPORTALS_TASK:-ClientPortalsTest} | Out-Null; 'started'"
for i in $(seq 1 90); do
  out=$($W <<< "if (Test-Path D:\ClientPortalsTest\harness\log.txt) { Get-Content D:\ClientPortalsTest\harness\log.txt | Select -Skip $n }" | tr -d '\r')
  echo "$out" | grep -q -E "starting world|joining " && { echo "$out" | grep -E "starting world|joining "; break; }
  sleep 4
done
