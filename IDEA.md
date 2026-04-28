# NetBench — HTTP Load Testing Tool

> Demo-приложение для демонстрации возможностей Plumix: параллелизм, Span<T>, структуры, real-time UI.

---

## Концепция

Инструмент нагрузочного тестирования HTTP API с полноценным GUI. Аналог `k6`, `hey`, `wrk` — но с нативным интерфейсом и гибридной архитектурой desktop + mobile.

**Целевая аудитория:** backend-разработчики, DevOps/SRE-инженеры, QA-инженеры.

**Ключевой посыл:** инструмент, который запускают на десктопе для полноценного анализа, и с телефона — чтобы быстро проверить прод во время дежурства.

---

## Архитектура

### Гибридная модель

```
┌─────────────────────────────────┐    ┌──────────────────────────────┐
│     Desktop (Avalonia + XAML)   │    │      Mobile (Plumix)         │
│                                 │    │                              │
│  • Редактор сценариев           │    │  • Быстрый запуск теста      │
│  • Real-time charts             │    │  • Просмотр результатов      │
│  • Latency heatmap              │    │  • Поделиться отчётом        │
│  • Детальный HTML/JSON отчёт    │    │  • Уведомления (ops-дежурство│
│  • История запусков             │    │                              │
└─────────────────────────────────┘    └──────────────────────────────┘
                     ↕ Shared Core (Plumix библиотека)
```

**Shared Core** содержит всю бизнес-логику: движок нагрузки, модели данных, агрегацию статистики. UI-слой — единственное что отличается между платформами.

### Бэкенд

- **Фаза 1 (MVP):** полностью локальное приложение. Никакого бэкенда. Сценарии и результаты хранятся на устройстве.
- **Фаза 2:** Supabase — синхронизация сценариев между устройствами, история запусков для команды, shared dashboards.

---

## Технические нюансы — почему это идеально для Plumix demo

### 1. Параллелизм как core-фича

Параллелизм здесь — не оптимизация, а смысл приложения. Без него инструмент не работает.

```
Сценарий: 500 concurrent users, 30 секунд, endpoint POST /api/orders

Реализация:
- 500 независимых Task'ов стартуют одновременно
- Каждый Task: отправить запрос → записать результат → повторить
- Координатор собирает результаты через Channel<RequestResult>
- UI обновляется каждые 250ms по агрегированным данным
```

Демонстрирует: `Task.WhenAll`, `Channel<T>`, `CancellationToken`, управление concurrency через `SemaphoreSlim`.

### 2. Span<T> и zero-alloc парсинг

При 10,000 req/s каждая лишняя аллокация — это давление на GC и latency spikes в результатах теста. Используем `Span<T>` для парсинга HTTP response без аллокаций.

```csharp
// Парсинг status code из response без string allocation
static int ParseStatusCode(ReadOnlySpan<byte> responseHeader)
{
    // "HTTP/1.1 200 OK" → ищем пробел, берём следующие 3 байта
    var spaceIdx = responseHeader.IndexOf((byte)' ');
    var codeSpan = responseHeader.Slice(spaceIdx + 1, 3);
    return (codeSpan[0] - '0') * 100 + (codeSpan[1] - '0') * 10 + (codeSpan[2] - '0');
}

// Парсинг Content-Length из headers без string split
static long ParseContentLength(ReadOnlySpan<byte> headers) { ... }
```

Демонстрирует: `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, zero-alloc patterns.

### 3. Структуры для результатов

`RequestResult` — struct, а не class. При 100,000 запросов за тест разница в памяти критична.

```csharp
readonly struct RequestResult
{
    public readonly long StartTimestampNs;   // 8 bytes
    public readonly long EndTimestampNs;     // 8 bytes  
    public readonly int  StatusCode;         // 4 bytes
    public readonly int  BytesReceived;      // 4 bytes
    public readonly bool IsError;            // 1 byte
    // Итого: ~25 bytes vs ~56 bytes для class (overhead объекта)
    
    public long LatencyNs => EndTimestampNs - StartTimestampNs;
    public double LatencyMs => LatencyNs / 1_000_000.0;
}
```

Хранение: `ArrayPool<RequestResult>` — переиспользуем буферы между запусками.

### 4. Real-time статистика без блокировок

Агрегатор статистики работает параллельно с движком нагрузки:

```
RequestResult[] → Channel<RequestResult> → StatisticsAggregator → UI-binding
                                                ↓
                                   Percentile вычисление через
                                   HDR Histogram (lock-free)
```

Вычисление p95/p99 на потоке данных — интересная алгоритмическая задача (HDR Histogram или t-digest).

---

## Функциональность

### Конфигуратор сценариев

```yaml
# Пример сценария
name: "Checkout API Load Test"
target: "https://api.example.com"

requests:
  - method: POST
    path: /api/orders
    headers:
      Authorization: "Bearer {{token}}"
      Content-Type: application/json
    body: |
      { "product_id": {{$randomInt 1 100}}, "quantity": 1 }

load:
  concurrent_users: 100
  duration: 60s
  ramp_up: 10s        # постепенно набираем нагрузку
  think_time: 500ms   # пауза между запросами одного "пользователя"
```

Поддержка переменных, случайных значений, цепочек запросов (результат первого запроса → параметр второго).

### Метрики в реальном времени

| Метрика | Описание |
|---------|----------|
| **Throughput** | Requests/sec в реальном времени |
| **Latency p50/p95/p99** | Персентили задержки |
| **Error rate** | % неуспешных запросов |
| **Active connections** | Текущие открытые соединения |
| **Bytes in/out** | Трафик |
| **Latency heatmap** | 2D-карта задержек по времени |

### Отчёт по завершении

- Сводка: min/max/mean/p50/p95/p99 latency
- График latency over time
- Разбивка по статус-кодам
- Топ медленных запросов
- Экспорт: JSON, HTML, CSV

---

## UI — Desktop (Avalonia)

### Раскладка

```
┌──────────────┬────────────────────────────────────────┐
│              │                                        │
│  Сценарии   │         Область результатов            │
│  (список)   │                                        │
│             │  [Throughput chart]  [Latency chart]   │
│  + Новый    │                                        │
│             │  [Heatmap]                             │
│             │                                        │
│             │  p50: 45ms   p95: 123ms   p99: 287ms  │
│             │  RPS: 847    Errors: 0.2%              │
│             │                                        │
├─────────────┴────────────────────────────────────────┤
│  [Конфигурация запуска]              [Run] [Stop]    │
└──────────────────────────────────────────────────────┘
```

### Ключевые Avalonia-контролы

- `ItemsControl` для списка сценариев
- `Canvas` или `LiveCharts2` для real-time графиков
- `DataGrid` для детальной таблицы результатов
- `ProgressBar` для ramp-up фазы
- Custom control для latency heatmap (2D grid с color coding)

---

## UI — Mobile (Plumix)

### Экраны

**1. Главный экран**
- Список сохранённых сценариев
- Кнопка "Быстрый тест" (ввести URL → запустить с дефолтными параметрами)

**2. Экран запуска**
- Выбор сценария
- Слайдер concurrent users (упрощённо)
- Большая кнопка RUN

**3. Экран мониторинга (во время теста)**
- Большие цифры: RPS, p95 latency, Error%
- Мини-график throughput
- Кнопка Stop

**4. Экран результатов**
- Сводная карточка с ключевыми метриками
- Поделиться (скриншот или JSON)

---

## Демонстрационные сценарии для показа

### Demo 1: "10,000 concurrent requests"
Запустить тест с высокой нагрузкой — показать что UI не подвисает, данные приходят плавно. Демонстрирует стабильность async/parallel архитектуры.

### Demo 2: "Сравнение двух endpoints"
Запустить два теста последовательно, сравнить результаты. Desktop-фича: side-by-side сравнение отчётов.

### Demo 3: "Mobile → Desktop"
Запустить быстрый тест с телефона, сохранить, открыть детальный отчёт на десктопе. Демонстрирует общий Shared Core и опциональную Supabase-синхронизацию.

### Demo 4: "Memory pressure"
Запустить тест с включённым GC Profiler — показать что zero-alloc подход на Span<T> даёт flat GC pressure при высокой нагрузке.

---

## Поэтапная разработка

### Фаза 1 — Core Engine
- [ ] `RequestResult` struct + `ArrayPool` буферы
- [ ] HTTP движок на `HttpClient` с parallel execution
- [ ] `Channel<RequestResult>` pipeline
- [ ] Базовая агрегация статистики (mean, percentiles)
- [ ] Сохранение сценариев локально (JSON файлы)

### Фаза 2 — Desktop UI (Avalonia)
- [ ] Редактор сценариев
- [ ] Real-time charts (LiveCharts2 или OxyPlot)
- [ ] Latency heatmap custom control
- [ ] HTML отчёт

### Фаза 3 — Mobile UI (Plumix)
- [ ] Список сценариев
- [ ] Экран мониторинга с large metrics
- [ ] Share результатов

### Фаза 4 — Sync (Supabase)
- [ ] Auth
- [ ] Синхронизация сценариев
- [ ] Облачное хранение истории запусков

---

## Название

**NetBench** — коротко, понятно, говорит само за себя.

Альтернативы: `Pulse`, `Hammr`, `Salvo`, `Barrage`

---

## Что это демонстрирует о Plumix

| Возможность Plumix | Как проявляется в приложении |
|--------------------|------------------------------|
| Shared Core между платформами | Весь движок нагрузки — одна библиотека |
| Производительный .NET на мобиле | 1000+ concurrent tasks на телефоне |
| Реактивный UI | Метрики обновляются 4 раза в секунду без лагов |
| Нативные контролы (Plumix) | Mobile UI ощущается нативным, не webview |
| Interop с Avalonia экосистемой | Desktop получает полный Avalonia toolset |
