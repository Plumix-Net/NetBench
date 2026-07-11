# NetBench

HTTP load-testing tool with a native GUI — an analog of `k6` / `hey` / `wrk`, built as a real-world showcase for [Plumix](https://github.com/Plumix-Net/Plumix): one codebase, two UI stacks, a zero-allocation load engine underneath.

[![CI](https://github.com/Plumix-Net/NetBench/actions/workflows/ci.yml/badge.svg)](https://github.com/Plumix-Net/NetBench/actions/workflows/ci.yml)
[![Plumix](https://img.shields.io/badge/built%20with-Plumix-blue)](https://plumix.net/)

Run load tests from your desktop for full analysis — or from your phone to quickly check prod during on-call duty.

## What it does

- **Scenarios** — configure target, requests, concurrent users, duration, ramp-up and think time; stored as local JSON.
- **Load engine** — hundreds of concurrent workers over `HttpClient`, results streamed through a bounded `Channel<RequestResult>`.
- **Real-time metrics** — throughput, latency percentiles (p50/p95/p99 via HDR Histogram) and error rate, charted live while the test runs.
- **Report** — summary and latency-over-time chart when the run completes.

The full concept and roadmap live in [IDEA.md](IDEA.md).

## One project, two UI stacks

The interesting part of this repo is the architecture: a single multi-targeted project serves every platform, and the platform picks its UI half at compile time — no `#if`s in code.

```
NetBench/                        # the only shared project
├── Features/<F>/                # clean architecture per feature
│   ├── Domain/                  # entities + contracts        (shared)
│   ├── Data/                    # repositories, load engine   (shared)
│   └── Presentation/
│       ├── Desktop/             # Avalonia views + MVVM ViewModels (CommunityToolkit)
│       └── Mobile/              # Plumix widgets + Cubit/State (Plumix.Bloc)
├── Desktop/                     # app shell for desktop: App, DI (Pure.DI), navigation, custom controls
└── Mobile/                      # app shell for mobile: MobileApp (PlumixApplication)
```

`NetBench.csproj` targets `net10.0;net10.0-android;net10.0-ios`. Platform heads are bootstrap-only and pick the nearest target:

| Head | Target | UI stack | State management |
|------|--------|----------|------------------|
| Desktop, Browser | `net10.0` | Avalonia controls (XAML) | MVVM, CommunityToolkit.Mvvm |
| Android, iOS | `net10.0-android` / `-ios` | [Plumix](https://github.com/Plumix-Net/Plumix) — Flutter-like widgets in C# | `Cubit<TState>` + immutable records ([Plumix.Bloc](https://www.nuget.org/packages/Plumix.Bloc)) |

Only the Domain and Data layers are shared between platforms; each UI stack brings its own state management. Conditional `Compile`/`AvaloniaXaml` items exclude the other platform's half from every compilation.

## Performance by design

Load generation is the whole point, so the hot path is allocation-free:

- `RequestResult` is a `readonly struct` passed by `in` — at 100k+ requests per run the difference versus a class is decisive.
- `Span<T>`/`ReadOnlySpan<T>` parsing, `ArrayPool` buffers, no LINQ on the hot path.
- Results flow through a bounded single-reader `Channel<RequestResult>`; aggregation uses lock-free counters (`Interlocked`) plus an HDR Histogram under a short lock.
- The UI only ever sees aggregated snapshots (~250 ms interval), never individual results.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). The shared project multi-targets mobile frameworks, so `android`/`ios` workloads are needed for a full restore — or pass `-p:MobileTargets=false` to build desktop-only without them.

```bash
# Run the desktop app
dotnet run --project NetBench/NetBench.Desktop/NetBench.Desktop.csproj

# Tests
dotnet test NetBench/NetBench.Tests/NetBench.Tests.csproj

# Without mobile workloads installed
dotnet build NetBench/NetBench.Desktop/NetBench.Desktop.csproj -p:MobileTargets=false

# Mobile (Plumix) flavor
dotnet build NetBench/NetBench/NetBench.csproj -f net10.0-android   # shared project only
dotnet build NetBench/NetBench.Android/NetBench.Android.csproj      # full APK
```

## See also

- [Plumix](https://github.com/Plumix-Net/Plumix) — Flutter-inspired UI framework for .NET
- [Plumix.Packages](https://github.com/Plumix-Net/Plumix.Packages) — Plumix.Bloc, Plumix.Provider
- [plumix.net](https://plumix.net/)
