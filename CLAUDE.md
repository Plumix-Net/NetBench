# NetBench

HTTP load-testing tool с GUI (аналог k6/hey/wrk) на Avalonia. Demo-приложение для Plumix: параллелизм, Span<T>, структуры, real-time UI. Полная концепция и roadmap — в [IDEA.md](IDEA.md).

## Команды

Всё выполняется из корня репозитория. Solution — `NetBench/NetBench.slnx`.

```bash
# Сборка desktop-цепочки — основной цикл разработки
dotnet build NetBench/NetBench.Desktop/NetBench.Desktop.csproj

# Тесты
dotnet test NetBench/NetBench.Tests/NetBench.Tests.csproj

# Запуск desktop-приложения
dotnet run --project NetBench/NetBench.Desktop/NetBench.Desktop.csproj

# Мобильный (Plumix) флейвор общего проекта — быстрая проверка компиляции
dotnet build NetBench/NetBench/NetBench.csproj -f net10.0-android

# Полная Android-голова (APK)
dotnet build NetBench/NetBench.Android/NetBench.Android.csproj
```

Общий проект `NetBench` мультитаргетный (`net10.0;net10.0-android;net10.0-ios`) — его restore требует установленных workloads android/ios. В окружении без workloads добавляй `-p:MobileTargets=false` (оставляет только `net10.0`) — так делает CI.

## Структура

Один общий проект: у каждой фичи свои domain/data/presentation-слои, платформенные головы — только bootstrap.

```
NetBench/
├── Directory.Build.props      # общие настройки: Nullable, ImplicitUsings, LangVersion, анализаторы
├── Directory.Packages.props   # central package management; версия Avalonia — $(AvaloniaVersion)
├── NetBench/                  # ЕДИНСТВЕННЫЙ общий проект (мультитаргет, TreatWarningsAsErrors)
│   ├── NetBench.csproj        # net10.0 → Desktop/Browser (Avalonia), net10.0-android/-ios → Mobile (Plumix);
│   │                          #   Desktop\** и Presentation\Desktop компилируются только в net10.0,
│   │                          #   Mobile\** и Presentation\Mobile — только в android/ios (условные Compile/AvaloniaXaml/пакеты)
│   ├── Features/              # clean architecture по фичам
│   │   ├── Scenarios/
│   │   │   ├── Domain/        #   LoadScenario, IScenarioRepository
│   │   │   ├── Data/          #   JsonScenarioRepository (source-generated JSON — trimming/AOT-safe)
│   │   │   └── Presentation/
│   │   │       ├── Desktop/   #     View.axaml + ViewModel (MVVM, CommunityToolkit)
│   │   │       └── Mobile/    #     Plumix-виджеты + Cubit/State (Plumix.Bloc)
│   │   ├── TestRun/
│   │   │   ├── Domain/        #   RequestResult (struct!), TestRunStats, StatisticsAggregator (HDR Histogram)
│   │   │   ├── Data/          #   LoadEngine: HttpClient + Channel<RequestResult> pipeline
│   │   │   └── Presentation/Desktop/
│   │   ├── Report/Presentation/Desktop/
│   │   └── Shell/Presentation/{Desktop,Mobile}   # Mobile: MobileShell — мобильный composition root
│   ├── Desktop/               # внефичевое desktop: App, ViewLocator, Views, Composition (Pure.DI),
│   │                          #   Services (Navigation), Controls (LineChart)
│   ├── Mobile/                # внефичевое mobile: MobileApp (PlumixApplication)
│   └── Assets/
├── NetBench.Tests/            # xUnit-тесты domain/data-слоёв (собираются против net10.0)
└── NetBench.{Desktop,Android,iOS,Browser}/  # головы, только bootstrap:
                               # Desktop/Browser → App, Android/iOS → MobileApp
```

## Принятые паттерны

- **Clean architecture по фичам:** все слои фичи — в `NetBench/Features/<F>/{Domain,Data,Presentation}`. Domain/Data общие для платформ и не зависят от UI; Presentation делится на `Desktop` и `Mobile`, стейт-менеджеры у каждой платформы свои.
- **Доменные модели — POCO:** никакого `INotifyPropertyChanged` и прочих UI-механик в Domain. Если XAML нужна реактивность — ObservableObject-обёртка в `Presentation/Desktop` с write-through в модель (пример — `ScenarioViewModel`).
- **Платформенное деление в csproj, не в коде:** никаких `#if ANDROID` — desktop- и mobile-половины исключаются из компиляции условными `Compile`/`AvaloniaXaml` Remove по `$(IsMobileTarget)`. Новые платформенные файлы клади в правильную папку (`Desktop\**`, `Mobile\**`, `Presentation\{Desktop,Mobile}`) — csproj подхватит сам.
- **Desktop (MVVM):** CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`). ViewModels — в `Presentation/Desktop` рядом с View.
- **Mobile (Plumix):** Flutter-подобные виджеты (`Plumix`, `Plumix.Material`), состояние — `Cubit<TState>` + иммутабельные record-состояния (`Plumix.Bloc`); зависимости — через `RepositoryProvider`/`BlocProvider` в `MobileShell`.
- **DI (desktop):** Pure.DI, конфигурация в `NetBench/Desktop/Composition/Composition.cs`. Новые ViewModel регистрируются там; для VM с runtime-аргументом — root вида `Func<TArg, TViewModel>`.
- **Bindings:** compiled bindings включены по умолчанию (`AvaloniaUseCompiledBindingsByDefault`) — в axaml указывай `x:DataType`.
- **Навигация (desktop):** через `INavigationService`.
- **Локализация:** строки генерирует Slang.Net из `Localization/i18n/*.i18n.json`. На мобиле читать
  их в виджетах только через `Translations<Strings>.Of(context)` (в обработчиках — `ReadOf`):
  подписка на `TranslationProvider<Strings>` из `MobileShell` — это то, что перестраивает дерево
  при смене локали. Прямой `Strings.Instance.Root` в `Build` не перерисуется; он допустим вне
  дерева (кубиты, `MobileApp`). Мост `Strings : ITranslations<Strings>` — в `Mobile/Localization/`,
  чтобы desktop-таргет не тянул `Plumix.Slang`.
- **Сериализация:** только source-generated `System.Text.Json` (`JsonSerializerContext`) — рефлексия ломает trimming на мобильных таргетах.
- **Пакеты:** версии только в `Directory.Packages.props` (CPM), в csproj — `PackageReference` без `Version`. Все Avalonia-пакеты — строго `$(AvaloniaVersion)`.
- **Общие настройки проектов** (Nullable, LangVersion и т.п.) — только в `Directory.Build.props`, не дублировать в csproj.

## Перф-правила движка (Features/TestRun) — не «рефакторить»

Производительность — смысл этого приложения. Эти решения приняты сознательно:

- `RequestResult` — `readonly struct`, передаётся по `in`. Не превращать в class/record.
- Горячий путь (запись результатов при 10k+ req/s) — без аллокаций: `Span<T>`/`ReadOnlySpan<T>` для парсинга, `ArrayPool` для буферов, никакого LINQ.
- Поток результатов — `Channel<RequestResult>` (bounded, SingleReader); агрегация — lock-free счётчики (`Interlocked`) + HDR Histogram под коротким lock.
- UI получает данные только агрегированными снапшотами (интервал ~250ms), а не по одному результату.
- В проекте `NetBench` включён `TreatWarningsAsErrors` (для всех таргетов).

## Правила разработки

- Новая логика в Domain/Data-слоях — вместе с тестами в `NetBench.Tests`.
- Слои не смешивать: Presentation не знает про `HttpClient`, гистограммы и файлы; Domain/Data не знают про Avalonia/Plumix.
- Код и комментарии — в стиле существующего кода; UI-тексты приложения на русском.
- CI (GitHub Actions): job `build-and-test` собирает Desktop (`-p:MobileTargets=false`) и гоняет тесты; job `build-mobile` ставит android workload и собирает Plumix-флейвор.
