#!/bin/bash

mkdir -p temp
mkdir -p generated

for proto in *.proto; do
    sed '1i\syntax = "proto2";' "$proto" > "temp/$proto"
done
echo $pwd
pushd temp

../protoc/protoc.exe \
    --proto_path="." \
    --cpp_out="../../Engine/src/proto" \
    --csharp_out="../../Sharp.Shared/Proto" \
    connectionless_netmessages.proto \
    cstrike15_gcmessages.proto \
    cstrike15_usermessages.proto \
    network_connection.proto \
    networkbasetypes.proto \
    engine_gcmessages.proto \
    steammessages.proto \
    gcsdk_gcmessages.proto \
    gameevents.proto \
    te.proto \
    usercmd.proto \
    usermessages.proto \
    cs_gameevents.proto \
    cs_usercmd.proto \
    netmessages.proto \
    source2_steam_stats.proto

popd

rm -rf temp
