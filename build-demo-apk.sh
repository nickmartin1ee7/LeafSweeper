#!/usr/bin/env bash
# Build the shareable demo APK into build/LeafSweeper.apk.
#
# One-time setup: create keystore.env in this folder with the release
# keystore password (see keystore.env.example).
#
# Usage: ./build-demo-apk.sh
set -euo pipefail
cd "$(dirname "$0")"

[ -f keystore.env ] && source keystore.env
: "${KS_PASS:?Create keystore.env with KS_PASS=<release keystore password>}"

KEYSTORE="${KEYSTORE:-$HOME/.local/share/godot/keystores/leafsweeper-release.keystore}"
GODOT="${GODOT:-$(command -v godot-mono || command -v godot || echo /nix/store/1pi31zgj0r85bbm2vn2afaaj0yc5pqcz-godot-mono-wrapper-4.7.1-stable/bin/godot-mono)}"

GODOT_ANDROID_KEYSTORE_RELEASE_PATH="$KEYSTORE" \
GODOT_ANDROID_KEYSTORE_RELEASE_USER="${KS_USER:-leafsweeper}" \
GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD="$KS_PASS" \
  "$GODOT" --headless --path . --export-release Android build/LeafSweeper.apk

# The export rewrites LeafSweeper.csproj; undo the churn.
git checkout -- LeafSweeper.csproj 2>/dev/null || true
rm -f LeafSweeper.csproj.old
echo "Done: build/LeafSweeper.apk"
