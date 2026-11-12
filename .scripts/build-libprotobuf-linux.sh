#!/usr/bin/env bash
# Builds Engine/lib/libprotobuf.a from the vendored protobuf tree (Linux).
# Run from a glibc <= 2.31 environment matching the CI/engine toolchain.
# Preferred: the same steamrt sniper SDK container the CI uses:
#   docker run --rm -v "$(pwd)":/repo \
#     registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest \
#     bash /repo/.scripts/build-libprotobuf-linux.sh
#
# Newer glibc (2.32+/2.38+) emits __libc_single_threaded / __isoc23_strto*
# references that do not resolve inside the sniper container, so linking
# modsharp.so fails there with --no-undefined. The sanity checks below reject
# such archives.
#
# The result must match the engine ABI:
#   - Release (-DNDEBUG): a non-NDEBUG build turns GOOGLE_DCHECK, e.g.
#     DCHECK(!field->options().weak()) in Reflection::SetBit/HasBit, into live
#     fatal checks that abort on engine-constructed messages
#   - _GLIBCXX_USE_CXX11_ABI=0: engine protobuf uses the pre-C++11 COW std::string
#   - -fPIC: the archive is linked into modsharp.so

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/Engine/vendors/protobuf"
OUT="$ROOT/Engine/lib/libprotobuf.a"
BUILD="${TMPDIR:-/tmp}/modsharp-libprotobuf-build"

if [ ! -f "$SRC/CMakeLists.txt" ]; then
    echo "ERROR: vendored protobuf tree not found at $SRC" >&2
    echo "       run: git submodule update --init" >&2
    exit 1
fi

# a stale cache from a different source path produces misleading results
if [ -f "$BUILD/CMakeCache.txt" ] && ! grep -qF "$SRC" "$BUILD/CMakeCache.txt"; then
    rm -rf "$BUILD"
fi

# -include cstdint: protobuf 3.21.x is missing <cstdint> includes under GCC 13+
# -ffile-prefix-map: keep build paths out of __FILE__ in GOOGLE_CHECK/LOG strings
#   so the archive is byte-identical regardless of where it was built
CXX_FLAGS="-D_GLIBCXX_USE_CXX11_ABI=0 -include cstdint -ffile-prefix-map=$SRC/="

cmake -S "$SRC" -B "$BUILD" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POSITION_INDEPENDENT_CODE=ON \
    -DCMAKE_CXX_FLAGS="$CXX_FLAGS" \
    -Dprotobuf_BUILD_TESTS=OFF \
    -Dprotobuf_BUILD_PROTOC_BINARIES=OFF \
    -Dprotobuf_BUILD_LIBPROTOC=OFF \
    -Dprotobuf_WITH_ZLIB=OFF

cmake --build "$BUILD" --target libprotobuf -j"$(nproc)"

# sanity: NDEBUG applied (no live DCHECK strings) and old string ABI only.
# Symbol names are plain text inside the archive, so grep the bytes directly
# instead of depending on binutils being installed.
if grep -qF "options().weak" "$BUILD/libprotobuf.a"; then
    echo "ERROR: debug assertions present - NDEBUG did not apply" >&2
    exit 1
fi
if grep -qF "_ZNSt7__cxx1112basic_string" "$BUILD/libprotobuf.a"; then
    echo "ERROR: cxx11 string ABI symbols found - _GLIBCXX_USE_CXX11_ABI=0 did not apply" >&2
    exit 1
fi
if ! grep -qF "_ZNSs" "$BUILD/libprotobuf.a"; then
    echo "ERROR: no COW string symbols found - unexpected toolchain output" >&2
    exit 1
fi
if grep -qF -e "__isoc23_strto" -e "__libc_single_threaded" "$BUILD/libprotobuf.a"; then
    echo "ERROR: archive references glibc 2.32+/2.38+ symbols and will not link" >&2
    echo "       inside the steamrt sniper container (glibc 2.31)." >&2
    echo "       Rebuild in a sniper SDK container or Ubuntu 20.04." >&2
    exit 1
fi
if grep -qF "$SRC" "$BUILD/libprotobuf.a"; then
    echo "ERROR: build paths leaked into the archive - -ffile-prefix-map did not apply" >&2
    exit 1
fi

cp "$BUILD/libprotobuf.a" "$OUT"
echo "OK: $OUT ($(du -h "$OUT" | cut -f1))"
