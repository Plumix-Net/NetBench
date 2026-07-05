# NetBench

HTTP load-testing tool с GUI (аналог k6/hey/wrk) на Avalonia. Demo-приложение для Plumix: параллелизм, Span<T>, структуры, real-time UI. Полная концепция и roadmap — в [IDEA.md](IDEA.md).

## Команды

Всё выполняется из корня репозитория. Solution — `NetBench/NetBench.slnx`.

```bash
# Сборка desktop-цепочки (Core → NetBench → Desktop) — основной цикл разработки
dotnet build NetBench/NetBench.Desktop/NetBench.Desktop.csproj

# Тесты
dotnet test NetBench/NetBench.Core.Tests/NetBench.Core.Tests.csproj

# Запуск desktop-приложения
dotnet run --project NetBench/NetBench.Desktop/NetBench.Desktop.csproj
```

Полный slnx (Android/iOS/Browser) требует установленных workloads — не собирай его без необходимости, платформенные головы проверяются отдельно.

## Структура

```
NetBench/
├── Directory.Build.props      # общие настройки: Nullable, ImplicitUsings, LangVersion, анализаторы
├── Directory.Packages.props   # central package management; версия Avalonia — $(AvaloniaVersion)
├── NetBench.Core/             # движок нагрузки, БЕЗ зависимостей на UI
│   ├── Engine/                # LoadEngine: HttpClient + Channel<RequestResult> pipeline
│   ├── Statistics/            # StatisticsAggregator: HDR Histogram, lock-free счётчики
│   ├── Models/                # RequestResult (struct!), LoadScenario, TestRunStats
│   └── Storage/               # ScenarioRepository (локальные JSON-файлы)
├── NetBench.Core.Tests/       # xUnit-тесты ядра
├── NetBench/                  # общий UI-проект (Avalonia)
│   ├── Features/              # feature-folders: Shell, Scenarios, TestRun, Report
│   │                          #   каждая фича = View.axaml + ViewModel рядом
│   ├── Composition/           # Pure.DI-контейнер (compile-time DI)
│   ├── Controls/              # кастомные контролы (LineChart)
│   └── Services/              # NavigationService и пр.
└── NetBench.{Desktop,Android,iOS,Browser}/  # платформенные головы, только bootstrap
```

## Принятые паттерны

- **MVVM:** CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`). ViewModels — в feature-папке рядом с View.
- **DI:** Pure.DI, конфигурация в `NetBench/Composition/Composition.cs`. Новые ViewModel регистрируются там; для VM с runtime-аргументом — root вида `Func<TArg, TViewModel>`.
- **Bindings:** compiled bindings включены по умолчанию (`AvaloniaUseCompiledBindingsByDefault`) — в axaml указывай `x:DataType`.
- **Навигация:** через `INavigationService`.
- **Пакеты:** версии только в `Directory.Packages.props` (CPM), в csproj — `PackageReference` без `Version`. Все Avalonia-пакеты — строго `$(AvaloniaVersion)`.
- **Общие настройки проектов** (Nullable, LangVersion и т.п.) — только в `Directory.Build.props`, не дублировать в csproj.

## Перф-правила ядра (NetBench.Core) — не «рефакторить»

Производительность — смысл этого приложения. Эти решения приняты сознательно:

- `RequestResult` — `readonly struct`, передаётся по `in`. Не превращать в class/record.
- Горячий путь (запись результатов при 10k+ req/s) — без аллокаций: `Span<T>`/`ReadOnlySpan<T>` для парсинга, `ArrayPool` для буферов, никакого LINQ.
- Поток результатов — `Channel<RequestResult>` (bounded, SingleReader); агрегация — lock-free счётчики (`Interlocked`) + HDR Histogram под коротким lock.
- UI получает данные только агрегированными снапшотами (интервал ~250ms), а не по одному результату.
- В `NetBench.Core` включён `TreatWarningsAsErrors`.

## Правила разработки

- Новая логика в Core — вместе с тестами в `NetBench.Core.Tests`.
- Бизнес-логика живёт только в Core; UI-проект не должен знать про HttpClient или гистограммы.
- Код и комментарии — в стиле существующего кода; UI-тексты приложения на русском.
- CI (GitHub Actions) собирает Desktop и гоняет тесты Core на каждый push/PR.
