#!/bin/bash
# =============================================================================
# ModSharp CS2 dedicated server entrypoint.
#
# 1. Install/update CS2 via steamcmd
# 2. Apply ModSharp overlay to the game directory
# 3. Patch gameinfo.gi to load ModSharp
# 4. Link engine .so files for ModSharp compatibility
# 5. Launch CS2 dedicated server
# =============================================================================
set -euo pipefail

CS2_DIR="${CS2_DIR:-/home/steam/cs2}"
STEAMCMD_DIR="${STEAMCMD_DIR:-/home/steam/steamcmd}"
OVERLAY_DIR="/opt/modsharp/overlay"

log()  { echo "[modsharp] $(date '+%Y-%m-%d %H:%M:%S') $*"; }
warn() { echo "[modsharp] $(date '+%Y-%m-%d %H:%M:%S') WARN: $*" >&2; }

# ── Steam client libraries ──────────────────────────────────────────────────
if [ ! -f "${STEAMCMD_DIR}/linux64/steamclient.so" ]; then
    log "Bootstrapping steamcmd..."
    "${STEAMCMD_DIR}/steamcmd.sh" +quit
fi
mkdir -p /home/steam/.steam/sdk64 /home/steam/.steam/sdk32
ln -sf "${STEAMCMD_DIR}/linux64/steamclient.so" /home/steam/.steam/sdk64/steamclient.so
ln -sf "${STEAMCMD_DIR}/linux32/steamclient.so" /home/steam/.steam/sdk32/steamclient.so

# ── Steamcmd app manifest ─────��─────────────────────────────────────────────
STEAM_APPS="/home/steam/Steam/steamapps"
PVC_STEAM_APPS="${CS2_DIR}/steamapps"
mkdir -p "${PVC_STEAM_APPS}" "${STEAM_APPS%/*}"
if [ ! -L "${STEAM_APPS}" ]; then
    rm -rf "${STEAM_APPS}"
    ln -sf "${PVC_STEAM_APPS}" "${STEAM_APPS}"
fi

# ── Install / Update CS2 ───────────���────────────────────────────────────────
if [ -f "${CS2_DIR}/game/bin/linuxsteamrt64/cs2" ] && [ "${FORCE_UPDATE:-0}" != "1" ]; then
    log "CS2 already installed. Set FORCE_UPDATE=1 to force update."
else
    log "Installing/updating CS2 via steamcmd..."
    "${STEAMCMD_DIR}/steamcmd.sh" \
        +force_install_dir "${CS2_DIR}" \
        +login anonymous \
        +app_update 730 \
        +quit
fi

# ── Apply ModSharp overlay ───────────��──────────────────────────────────────
if [ -d "${OVERLAY_DIR}/sharp" ]; then
    log "Applying ModSharp overlay..."
    mkdir -p "${CS2_DIR}/game"
    cp -rf "${OVERLAY_DIR}/." "${CS2_DIR}/game/"
    log "ModSharp overlay applied."
fi

# ── Patch gameinfo.gi ─���─────────────────────────────────────────────────────
GAMEINFO="${CS2_DIR}/game/csgo/gameinfo.gi"
if [ -f "${GAMEINFO}" ]; then
    sed -i "/csgo\/sharp/d" "${GAMEINFO}"
    sed -i "/csgo\/addons\/metamod/d" "${GAMEINFO}"
    if ! grep -q "Game[[:space:]]*sharp" "${GAMEINFO}"; then
        sed -i '/Game_LowViolence.*csgo_lv/a\\t\t\tGame\tsharp' "${GAMEINFO}"
        log "Patched gameinfo.gi: added Game sharp"
    fi
fi

# ── Link engine .so files ───────────────────────────────────────────────────
ENGINE_BIN="${CS2_DIR}/game/bin/linuxsteamrt64"
CSGO_BIN="${CS2_DIR}/game/csgo/bin/linuxsteamrt64"
if [ -d "${ENGINE_BIN}" ] && [ -d "${CSGO_BIN}" ]; then
    for so in "${ENGINE_BIN}"/*.so; do
        target="${CSGO_BIN}/$(basename "$so")"
        [ ! -e "${target}" ] && ln -sf "$so" "${target}"
    done
    log "Engine .so files linked to csgo/bin"
fi

# ── Launch CS2 ──────────���───────────────────────────────────────────────────
log "Starting CS2 server..."
log "  Map:        ${MAP:-de_dust2}"
log "  Port:       ${PORT:-27015}"
log "  MaxPlayers: ${MAXPLAYERS:-32}"

exec "${CS2_DIR}/game/bin/linuxsteamrt64/cs2" \
    -dedicated \
    -console \
    -usercon \
    -port "${PORT:-27015}" \
    +map "${MAP:-de_dust2}" \
    +sv_setsteamaccount "${STEAM_ACCOUNT}" \
    +rcon_password "${RCON_PASSWORD}" \
    +sv_visiblemaxplayers "${MAXPLAYERS:-32}" \
    +game_type "${GAME_TYPE:-0}" \
    +game_mode "${GAME_MODE:-0}" \
    +hostname "${SERVER_NAME:-ModSharp CS2 Server}"
