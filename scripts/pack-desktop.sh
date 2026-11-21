#!/usr/bin/env bash
# Собирает desktop-версию NetBench (Native AOT) под одну платформу и упаковывает её Velopack'ом:
#   win-x64, win-arm64     → NetBench-win-*-Setup.exe, -Portable.zip
#   osx-arm64, osx-x64     → NetBench-osx-*-Setup.pkg, -Portable.zip (NetBench.app)
#   linux-x64, linux-arm64 → NetBench-linux-*.AppImage
# плюс пакеты обновления (*-full.nupkg, releases.<канал>.json) для автообновления из GitHub Releases.
# Сборку для Mac App Store делает scripts/pack-mac-appstore.sh.
#
# Запуск из корня репозитория на целевой ОС и архитектуре (Native AOT и vpk собирают под свою ОС;
# osx-x64 на Apple Silicon собирается кросс-компиляцией):
#   scripts/pack-desktop.sh osx-arm64
#
# Native AOT требует: Windows — Visual Studio с «Desktop development with C++»; Linux — clang и zlib1g-dev;
# macOS — Xcode и workload macos (.app собирает net11.0-macos-голова).
#
# Подпись macOS (иначе — без подписи, Gatekeeper ругается при первом запуске):
#   MACOS_SIGN_APP_IDENTITY="Developer ID Application: …"
#   MACOS_SIGN_INSTALL_IDENTITY="Developer ID Installer: …"  — без него .pkg не собирается (только .app в zip)
#   MACOS_NOTARY_PROFILE=<профиль xcrun notarytool store-credentials> — нотаризация
# Дельты к прошлой версии: VPK_GITHUB_TOKEN — перед упаковкой скачивается последний релиз.
set -euo pipefail

RID="${1:?usage: scripts/pack-desktop.sh <win-x64|win-arm64|osx-arm64|osx-x64|linux-x64|linux-arm64>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

PROJECT=NetBench/NetBench.Desktop/NetBench.Desktop.csproj
MAC_DIR=NetBench/NetBench.Desktop/macOS
OUT="artifacts/desktop/$RID"
PUBLISH="$OUT/publish"
RELEASES="$OUT/releases"
REPO_URL=https://github.com/Plumix-Net/NetBench

VERSION="$(dotnet msbuild "$PROJECT" -getProperty:NetBenchVersion -p:MobileTargets=false -p:MacTarget=false)"
CHANNEL="$RID"

rm -rf "$OUT"
mkdir -p "$OUT"
dotnet tool restore >/dev/null

args=(--packId NetBench --packVersion "$VERSION" --packTitle NetBench --packAuthors Plumix-Net
      --channel "$CHANNEL" --outputDir "$RELEASES"
      # Символы Native AOT (.dbg/.dSYM, сотни МБ) и pdb в установщик не кладём.
      --exclude '\.(pdb|dbg)$')
# Заметки версии — в пакет обновления и тело GitHub Release (vpk upload github).
scripts/release-notes.sh "$VERSION" > "$OUT/release-notes.md"
args+=(--releaseNotes "$OUT/release-notes.md")

case "$RID" in
  win-*|linux-*)
    dotnet publish "$PROJECT" -f net11.0 -c Release -r "$RID" --self-contained \
      -p:MobileTargets=false -p:MacTarget=false -o "$PUBLISH"
    args+=(--packDir "$PUBLISH")
    if [[ "$RID" == win-* ]]; then
      args+=(--mainExe NetBench.Desktop.exe --icon NetBench/NetBench/Assets/netbench.ico)
    else
      args+=(--mainExe NetBench.Desktop --icon NetBench/NetBench.Desktop/Packaging/netbench.png
             --categories "Development;Network;Utility")
    fi
    ;;
  osx-*)
    # .app (Info.plist, иконка из каталога ассетов) собирает macOS-голова. Без -o: с абсолютным
    # каталогом вывода SDK собирает бандл неправильно. Подписывает и нотаризует vpk — с hardened runtime.
    rm -rf "NetBench/NetBench.Desktop/bin/Release/net11.0-macos/$RID" "NetBench/NetBench.Desktop/obj/Release/net11.0-macos/$RID"
    dotnet publish "$PROJECT" -f net11.0-macos -c Release -r "$RID" -p:MobileTargets=false
    APP="NetBench/NetBench.Desktop/bin/Release/net11.0-macos/$RID/NetBench.app"
    [[ -d "$APP" ]] || { echo "app bundle not found: $APP" >&2; exit 1; }
    args+=(--packDir "$APP" --mainExe NetBench.Desktop --signEntitlements "$MAC_DIR/DeveloperId.entitlements")
    [[ -n "${MACOS_SIGN_APP_IDENTITY:-}" ]] && args+=(--signAppIdentity "$MACOS_SIGN_APP_IDENTITY")
    if [[ -n "${MACOS_SIGN_INSTALL_IDENTITY:-}" ]]; then
      args+=(--signInstallIdentity "$MACOS_SIGN_INSTALL_IDENTITY")
    elif [[ -n "${MACOS_SIGN_APP_IDENTITY:-}" ]]; then
      # Неподписанный .pkg Gatekeeper не откроет — отдаём только подписанный .app в zip.
      args+=(--noInst)
    fi
    [[ -n "${MACOS_NOTARY_PROFILE:-}" ]] && args+=(--notaryProfile "$MACOS_NOTARY_PROFILE")
    ;;
  *)
    echo "unknown RID: $RID" >&2
    exit 1
    ;;
esac

if [[ -n "${VPK_GITHUB_TOKEN:-}" ]]; then
  dotnet vpk download github --repoUrl "$REPO_URL" --token "$VPK_GITHUB_TOKEN" \
    --channel "$CHANNEL" --outputDir "$RELEASES" || echo "no previous release for $CHANNEL — full package only"
fi

dotnet vpk pack "${args[@]}"
ls -la "$RELEASES"
