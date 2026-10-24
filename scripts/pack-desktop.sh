#!/usr/bin/env bash
# Собирает desktop-версию NetBench под одну платформу и упаковывает её Velopack'ом:
#   win-x64            → NetBench-win-x64-Setup.exe, -Portable.zip
#   osx-arm64, osx-x64 → NetBench-osx-*-Setup.pkg, -Portable.zip (NetBench.app)
#   linux-x64          → NetBench-linux-x64.AppImage
# плюс пакеты обновления (*-full.nupkg, releases.<канал>.json) для автообновления из GitHub Releases.
#
# Запуск из корня репозитория на целевой ОС (vpk пакует только под свою ОС):
#   scripts/pack-desktop.sh osx-arm64
#
# Подпись macOS (иначе — без подписи, Gatekeeper ругается при первом запуске):
#   MACOS_SIGN_APP_IDENTITY="Developer ID Application: …"
#   MACOS_SIGN_INSTALL_IDENTITY="Developer ID Installer: …"  — без него .pkg не собирается (только .app в zip)
#   MACOS_NOTARY_PROFILE=<профиль xcrun notarytool store-credentials> — нотаризация
# Дельты к прошлой версии: VPK_GITHUB_TOKEN — перед упаковкой скачивается последний релиз.
set -euo pipefail

RID="${1:?usage: scripts/pack-desktop.sh <win-x64|osx-arm64|osx-x64|linux-x64>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

PROJECT=NetBench/NetBench.Desktop/NetBench.Desktop.csproj
PACKAGING=NetBench/NetBench.Desktop/Packaging
OUT="artifacts/desktop/$RID"
PUBLISH="$OUT/publish"
RELEASES="$OUT/releases"
REPO_URL=https://github.com/Plumix-Net/NetBench

VERSION="$(dotnet msbuild "$PROJECT" -getProperty:NetBenchVersion -p:MobileTargets=false)"
CHANNEL="$RID"

rm -rf "$OUT"
mkdir -p "$OUT"
dotnet tool restore >/dev/null
dotnet publish "$PROJECT" -c Release -r "$RID" --self-contained -p:MobileTargets=false -o "$PUBLISH"

args=(--packId NetBench --packVersion "$VERSION" --packTitle NetBench --packAuthors Plumix-Net
      --packDir "$PUBLISH" --channel "$CHANNEL" --outputDir "$RELEASES")
# Заметки версии — в пакет обновления и тело GitHub Release (vpk upload github).
scripts/release-notes.sh "$VERSION" > "$OUT/release-notes.md"
args+=(--releaseNotes "$OUT/release-notes.md")

case "$RID" in
  win-*)
    args+=(--mainExe NetBench.Desktop.exe --icon NetBench/NetBench/Assets/netbench.ico)
    ;;
  osx-*)
    args+=(--mainExe NetBench.Desktop --icon "$PACKAGING/netbench.icns" --bundleId net.plumix.netbench.desktop
           --signEntitlements "$PACKAGING/netbench.entitlements")
    [[ -n "${MACOS_SIGN_APP_IDENTITY:-}" ]] && args+=(--signAppIdentity "$MACOS_SIGN_APP_IDENTITY")
    if [[ -n "${MACOS_SIGN_INSTALL_IDENTITY:-}" ]]; then
      args+=(--signInstallIdentity "$MACOS_SIGN_INSTALL_IDENTITY")
    elif [[ -n "${MACOS_SIGN_APP_IDENTITY:-}" ]]; then
      # Неподписанный .pkg Gatekeeper не откроет — отдаём только подписанный .app в zip.
      args+=(--noInst)
    fi
    [[ -n "${MACOS_NOTARY_PROFILE:-}" ]] && args+=(--notaryProfile "$MACOS_NOTARY_PROFILE")
    ;;
  linux-*)
    args+=(--mainExe NetBench.Desktop --icon "$PACKAGING/netbench.png")
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
