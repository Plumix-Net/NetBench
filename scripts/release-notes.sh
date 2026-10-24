#!/usr/bin/env bash
# Печатает раздел CHANGELOG.md для версии (без заголовка): scripts/release-notes.sh 1.0.0
set -euo pipefail

VERSION="${1:?usage: scripts/release-notes.sh <version>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

notes="$(awk -v heading="## v$VERSION" '
  $0 == heading || index($0, heading " ") == 1 { found = 1; next }
  found && /^## / { exit }
  found { print }
' "$ROOT/CHANGELOG.md" | sed -e '/./,$!d')"

if [[ -z "$notes" ]]; then
  echo "CHANGELOG.md has no section for v$VERSION" >&2
  exit 1
fi
printf '%s\n' "$notes"
