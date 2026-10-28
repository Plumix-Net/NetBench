#!/usr/bin/env bash
# E2E-тесты мобильного NetBench (Maestro, флоу в e2e/) на Android-эмуляторе или iOS-симуляторе.
# Скрипт собирает и ставит приложение, поднимает локальную цель для прогонов и запускает флоу.
#
# Запуск из корня репозитория, эмулятор/симулятор уже загружен:
#   scripts/e2e.sh android
#   scripts/e2e.sh ios
#   scripts/e2e.sh android --no-build                 # без пересборки, только переустановка
#   scripts/e2e.sh ios --no-build e2e/flows/home.yaml # один флоу (пути и прочие аргументы — в maestro test)
#
# Устройство: ANDROID_SERIAL / IOS_SIMULATOR (UDID), иначе первое подключённое / загруженное.
# Нужны maestro (brew install mobile-dev-inc/tap/maestro), Java 17+, python3, workloads android/ios.
# Отчёт JUnit, скриншоты и логи упавших шагов — в artifacts/e2e/<платформа>/.
set -euo pipefail

PLATFORM="${1:?usage: scripts/e2e.sh <android|ios> [--no-build] [maestro test args...]}"
shift
BUILD=true
if [[ "${1:-}" == "--no-build" ]]; then
  BUILD=false
  shift
fi

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

APP_ID="$(dotnet msbuild NetBench/NetBench.Android/NetBench.Android.csproj -getProperty:ApplicationId)"
PORT="${E2E_TARGET_PORT:-8099}"
OUT="artifacts/e2e/$PLATFORM"
rm -rf "$OUT"
mkdir -p "$OUT/www"

case "$PLATFORM" in
  android)
    DEVICE="${ANDROID_SERIAL:-$(adb devices | awk 'NR > 1 && $2 == "device" { print $1; exit }')}"
    [[ -n "$DEVICE" ]] || { echo "no Android device: start an emulator or set ANDROID_SERIAL" >&2; exit 1; }
    # Сборки в APK целиком: fast deployment раскладывает сборки по другому устройству, когда их несколько
    APK=NetBench/NetBench.Android/bin/Debug/net10.0-android/$APP_ID-Signed.apk
    if $BUILD; then
      dotnet build NetBench/NetBench.Android/NetBench.Android.csproj -c Debug \
        -p:EmbedAssembliesIntoApk=true -p:AndroidPackageFormat=apk
    fi
    adb -s "$DEVICE" install -r "$APK"
    # Системные анимации — главный источник нестабильных ожиданий; на эмуляторе для тестов их выключают
    for scale in window_animation_scale transition_animation_scale animator_duration_scale; do
      adb -s "$DEVICE" shell settings put global "$scale" 0
    done
    # Цель на 127.0.0.1 одинакова для обеих платформ: эмулятору её пробрасывает adb reverse
    adb -s "$DEVICE" reverse "tcp:$PORT" "tcp:$PORT"
    ;;
  ios)
    DEVICE="${IOS_SIMULATOR:-$(xcrun simctl list devices booted -j \
      | python3 -c 'import json, sys; print(next((d["udid"] for r in json.load(sys.stdin)["devices"].values() for d in r if d["state"] == "Booted"), ""))')}"
    [[ -n "$DEVICE" ]] || { echo "no booted iOS simulator: boot one or set IOS_SIMULATOR" >&2; exit 1; }
    # Release: Debug-сборка под симулятор падает при старте рантайма Mono, ещё до кода приложения
    APP=NetBench/NetBench.iOS/bin/Release/net10.0-ios/iossimulator-arm64/NetBench.iOS.app
    if $BUILD; then
      dotnet build NetBench/NetBench.iOS/NetBench.iOS.csproj -c Release -r iossimulator-arm64
    fi
    xcrun simctl install "$DEVICE" "$APP"
    ;;
  *)
    echo "unknown platform: $PLATFORM (android|ios)" >&2
    exit 1
    ;;
esac

# Цель прогонов — статический HTTP-сервер на loopback: только свой сервер, как требует RunConsent
echo ok > "$OUT/www/index.html"
python3 -m http.server "$PORT" --bind 127.0.0.1 --directory "$OUT/www" > "$OUT/target.log" 2>&1 &
TARGET_PID=$!
trap 'kill $TARGET_PID 2>/dev/null || true' EXIT

# Без флоу в аргументах — весь набор из e2e/ (порядок и фильтры — e2e/config.yaml)
args=("$@")
has_flow=false
for arg in "${args[@]+"${args[@]}"}"; do
  [[ -e "$arg" ]] && has_flow=true
done
$has_flow || args+=(e2e)
maestro --device "$DEVICE" test \
  --env "TARGET=http://127.0.0.1:$PORT/" \
  --format junit --output "$OUT/report.xml" \
  --test-output-dir "$OUT" \
  "${args[@]}"
