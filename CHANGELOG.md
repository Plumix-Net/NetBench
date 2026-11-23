# Changelog

Newest first. One line per user-visible change. On release the version section becomes the GitHub Release
notes and the desktop update notes (`scripts/release-notes.sh`); store "What's new" texts are written
separately per language in `fastlane/metadata/ios/*/release_notes.txt`.

## Unreleased
- Android and iOS: native builds (Native AOT) — about half the download size and a faster start.

## v1.1.0
- Android: NetBench runs on Plumix's native Android host (Impeller rendering, no Avalonia): a smaller app and faster start.
- iOS: NetBench runs on Plumix's native iOS host (Impeller on Metal, no Avalonia), Plumix 1.0.0-preview.9.
- Desktop: native builds (Native AOT) — faster start and smaller downloads; new Windows on Arm and Linux arm64 builds.
- Desktop: NetBench asks to confirm you test only your own servers before the first run, as on mobile.
- macOS: NetBench comes to the Mac App Store (universal app for Apple silicon and Intel); macOS 14 or later.
- Desktop: a Target URL without http(s):// (e.g. `google.com`) no longer produces millions of instant "errors" and
  a meaningless RPS: the editor adds https:// or rejects the address, and the engine refuses such a run.
- Desktop: the run screen shows live RPS and P95 for the last moment (charts no longer flatten into an average),
  ramp-up progress (virtual users) and why requests fail; the report shows average RPS, totals and a breakdown of
  requests without a response (DNS, refused, timeout, TLS…) with a hint and the exact error.
- Latency percentiles count only requests the server answered — failed connections no longer drag them to 0 ms;
  a run stops at once when a request can't be sent at all (bad URL, method or header).
- Desktop: run history survives a restart; leaving the run screen stops the run instead of loading in the background.
- macOS: real app menu — NetBench (About NetBench), File (new/edit scenario, run, stop, export), Edit, View, Window,
  Help — instead of "Avalonia Application"; the same shortcuts with Ctrl on Windows and Linux.
- Desktop: "Edit" on the report, "Saved" in the editor, Run from the editor saves the draft first; icon buttons
  have names for VoiceOver and screen readers.
- Mobile: the live monitor shows the current RPS instead of the average since start.
- Desktop: long values fit their metric cards and the report header no longer cuts off the address and date.

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
