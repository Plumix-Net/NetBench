#!/usr/bin/env bash
# Собирает NetBench для Mac App Store: универсальный (arm64 + x86_64) Native AOT .app в песочнице,
# подписанный «Apple Distribution» с профилем Mac App Store, и .pkg, подписанный
# «3rd Party Mac Developer Installer», — то, что принимает App Store Connect.
#
#   scripts/pack-mac-appstore.sh <build number> <имя профиля Mac App Store>
#
# Обычно запускается из fastlane (bundle exec fastlane mac beta): он сам скачивает профиль и
# считает build number. Результат — artifacts/mac-appstore/NetBench-<версия>.pkg.
# Сертификаты — из связки ключей (локально) или из временной связки fastlane (CI).
set -euo pipefail

BUILD_NUMBER="${1:?usage: scripts/pack-mac-appstore.sh <build number> <provisioning profile name>}"
PROFILE="${2:?usage: scripts/pack-mac-appstore.sh <build number> <provisioning profile name>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

PROJECT=NetBench/NetBench.Desktop/NetBench.Desktop.csproj
OUT=artifacts/mac-appstore

# Флейвор App Store меняет bundle id, entitlements и состав сборки — старые объекты Release не годятся.
rm -rf "$OUT" NetBench/NetBench.Desktop/bin/Release/net11.0-macos NetBench/NetBench.Desktop/obj/Release/net11.0-macos
mkdir -p "$OUT"

# Без -o: macOS SDK собирает бандл неправильно с абсолютным каталогом вывода.
dotnet publish "$PROJECT" -f net11.0-macos -c Release -p:MobileTargets=false \
  -p:NetBenchAppStore=true \
  -p:NetBenchBuildNumber="$BUILD_NUMBER" \
  "-p:CodesignKey=Apple Distribution" \
  "-p:CodesignProvision=$PROFILE" \
  "-p:PackageSigningKey=3rd Party Mac Developer Installer"

PKG=$(find NetBench/NetBench.Desktop/bin/Release/net11.0-macos -name "*.pkg" -path "*publish*" | head -1)
[[ -n "$PKG" ]] || { echo "pkg not found" >&2; exit 1; }
cp "$PKG" "$OUT/NetBench-$(basename "$PKG" | sed -E 's/.*-([0-9.]+)\.pkg$/\1/').pkg"
# Универсальный бандл — в корне net11.0-macos (в osx-*/ лежат промежуточные по архитектурам).
APP=NetBench/NetBench.Desktop/bin/Release/net11.0-macos/NetBench.app

echo "--- $APP"
lipo -info "$APP/Contents/MacOS/NetBench.Desktop"
codesign -d --entitlements - --xml "$APP" 2>/dev/null | plutil -convert xml1 -o - - | grep -E "<key>" || true
pkgutil --check-signature "$OUT"/*.pkg | head -3
ls -la "$OUT"
