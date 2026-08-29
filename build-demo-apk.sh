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

# Godot reads the SDK paths from editor settings; headless runs can drop or
# zero them (godotengine/godot#76559 family of EditorSettings bugs). Repair
# the keys inside the [resource] section before each export, and set
# JAVA_HOME/ANDROID_HOME as a fallback default for fresh settings.
# java_sdk_path uses the stable NixOS system path so nix store garbage
# collection can't break it (override with ANDROID_SDK_PATH / JAVA_SDK_PATH).
SETTINGS="$HOME/.config/godot/editor_settings-4.7.tres"
ANDROID_SDK="${ANDROID_SDK_PATH:-$HOME/Android/Sdk}"
JAVA_SDK="${JAVA_SDK_PATH:-/run/current-system/sw/lib/openjdk}"
if [ ! -x "$JAVA_SDK/bin/java" ]; then
  JAVA_SDK="$(dirname "$(dirname "$(readlink -f "$(command -v java)")")")"
fi
export JAVA_HOME="$JAVA_SDK" ANDROID_HOME="$ANDROID_SDK"
if [ -f "$SETTINGS" ]; then
  sed -i '/^export\/android\/android_sdk_path /d; /^export\/android\/java_sdk_path /d' "$SETTINGS"
  sed -i "/^\[resource\]/a export/android/android_sdk_path = \"$ANDROID_SDK\"\nexport/android/java_sdk_path = \"$JAVA_SDK\"" "$SETTINGS"
fi

# Remove any stale APK so a failed export can't masquerade as success below.
rm -f build/LeafSweeper.apk

# Godot headless exports always print a benign shutdown error (see
# godotengine/godot#76559); hide exactly that so real errors stand out.
GODOT_ANDROID_KEYSTORE_RELEASE_PATH="$KEYSTORE" \
GODOT_ANDROID_KEYSTORE_RELEASE_USER="${KS_USER:-leafsweeper}" \
GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD="$KS_PASS" \
  "$GODOT" --headless --path . --export-release Android build/LeafSweeper.apk 2>&1 |
  grep -vF -e 'ERROR: EditorSettings not instantiated yet when getting setting "export/android/shutdown_adb_on_exit".' \
            -e '   at: _EDITOR_GET (editor/settings/editor_settings.cpp:1656)' || true

# Fail loudly if the export didn't actually produce the APK.
[ -f build/LeafSweeper.apk ] || { echo "Export FAILED: build/LeafSweeper.apk missing" >&2; exit 1; }

# The export rewrites LeafSweeper.csproj; undo the churn.
git checkout -- LeafSweeper.csproj 2>/dev/null || true
rm -f LeafSweeper.csproj.old
echo "Done: build/LeafSweeper.apk"
