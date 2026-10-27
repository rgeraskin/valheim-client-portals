#!/usr/bin/env bash
# Copies what the build compiles against into .deps/ from a Windows install over SSH:
# the game's Managed/ assemblies and BepInEx/core from a mod manager profile.
#   CLIENTPORTALS_REMOTE=user@host CLIENTPORTALS_PROFILE=<Gale profile> tools/fetch-local-deps.sh
set -euo pipefail
cd "$(dirname "$0")/.."
remote="${CLIENTPORTALS_REMOTE:?set CLIENTPORTALS_REMOTE=user@host, e.g. in mise.local.toml}"
profile="${CLIENTPORTALS_PROFILE:?set CLIENTPORTALS_PROFILE to a Gale profile name, e.g. in mise.local.toml}"
rm -rf .deps && mkdir -p .deps/valheim_Data .deps/BepInEx
scp -q -r "$remote:C:/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/Managed" .deps/valheim_Data/
scp -q -r "$remote:AppData/Roaming/com.kesomannen.gale/valheim/profiles/$profile/BepInEx/core" .deps/BepInEx/
echo "deps in .deps/"
