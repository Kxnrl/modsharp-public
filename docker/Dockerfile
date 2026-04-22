# =============================================================================
# ModSharp CS2 Dedicated Server Docker Image
#
# Pre-installs ModSharp framework into a CS2 dedicated server image.
# Server operators can extend this image with their own plugins, configs,
# and maps by adding layers or mounting volumes.
#
# Build:
#   docker build -t modsharp-cs2:latest -f docker/Dockerfile .
#
# Run:
#   docker run -d \
#     -p 27015:27015/udp \
#     -e STEAM_ACCOUNT=<your_gslt> \
#     -e RCON_PASSWORD=<your_rcon_pw> \
#     -v cs2-data:/home/steam/cs2 \
#     modsharp-cs2:latest
# =============================================================================

FROM registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest

LABEL maintainer="ModSharp dev team"
LABEL description="CS2 dedicated server with ModSharp framework pre-installed"

USER root

RUN dpkg --add-architecture i386 && \
    apt-get update && apt-get install -y --no-install-recommends \
        curl ca-certificates lib32gcc-s1 \
    && rm -rf /var/lib/apt/lists/*

# Create steam user
RUN useradd -m -s /bin/bash steam

# Install steamcmd
RUN mkdir -p /home/steam/steamcmd && \
    curl -fsSL https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz | \
    tar xzf - -C /home/steam/steamcmd && \
    chown -R steam:steam /home/steam

# .NET 10.0 runtime (required by ModSharp)
ARG DOTNET_VERSION="10.0.5"
RUN curl -fsSL "https://dotnetcli.azureedge.net/dotnet/Runtime/${DOTNET_VERSION}/dotnet-runtime-${DOTNET_VERSION}-linux-x64.tar.gz" \
        -o /tmp/dotnet-runtime.tar.gz \
    && mkdir -p /opt/modsharp/overlay/sharp/runtime \
    && tar xzf /tmp/dotnet-runtime.tar.gz -C /opt/modsharp/overlay/sharp/runtime/ \
    && rm /tmp/dotnet-runtime.tar.gz

# Copy the ModSharp overlay (built by CI or provided at build time).
# The overlay contains: sharp/{bin,core,configs,gamedata,locales,modules,shared,extensions}/
COPY --chown=steam:steam build/ /opt/modsharp/overlay/

# Copy entrypoint
COPY --chown=steam:steam docker/entrypoint.sh /opt/modsharp/entrypoint.sh
RUN chmod +x /opt/modsharp/entrypoint.sh

# Switch to steam user
USER steam

ENV CS2_DIR="/home/steam/cs2" \
    PORT="27015" \
    MAXPLAYERS="32" \
    MAP="de_dust2" \
    GAME_TYPE="0" \
    GAME_MODE="0" \
    STEAM_ACCOUNT="" \
    RCON_PASSWORD="" \
    SERVER_NAME="ModSharp CS2 Server"

EXPOSE 27015/tcp 27015/udp

ENTRYPOINT ["/opt/modsharp/entrypoint.sh"]
