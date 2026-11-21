# Changelog

Newest first. One line per user-visible change. On release the version section becomes the GitHub Release
notes and the desktop update notes (`scripts/release-notes.sh`); store "What's new" texts are written
separately per language in `fastlane/metadata/ios/*/release_notes.txt`.

## Unreleased
- Desktop: native builds (Native AOT) — faster start and smaller downloads; new Windows on Arm and Linux arm64 builds.
- Desktop: NetBench asks to confirm you test only your own servers before the first run, as on mobile.
- macOS: NetBench comes to the Mac App Store (universal app for Apple silicon and Intel); macOS 14 or later.

## v1.0.1
- Android: small text no longer turns into block glyphs after moving between screens (Plumix 1.0.0-preview.2).
- Android 10–14: status and navigation bars are transparent again, and no dark frame outlines the screen
  (Plumix 1.0.0-preview.3).
- Android: text stays sharp while a list stretches at its end; latency bars have round ends (Plumix 1.0.0-preview.3).
- Android: the back swipe closes the screen again (Plumix 1.0.0-preview.4).

## v1.0.0
- First release: Android and iOS (Plumix UI) with quick test, scenarios, live monitor, run history and shareable
  reports (text, HTML, CSV, JSON); runs from a phone are limited to 50 users and 5 minutes.
- Desktop (Windows, macOS, Linux; Avalonia UI): installers with automatic updates from GitHub Releases.
