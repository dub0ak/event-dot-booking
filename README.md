# EBooking API

## Описание

EBooking — REST API для управления мероприятиями и бронированиями, реализованный на ASP.NET Core Web API.

Проект позволяет создавать мероприятия, управлять списком событий, выполнять бронирование мест и отслеживать статус бронирований. Для хранения данных используется PostgreSQL, а доступ к данным реализован через Entity Framework Core.

---

## Возможности

### Работа с мероприятиями

- Создание мероприятий
- Получение списка мероприятий
- Получение мероприятия по идентификатору
- Обновление мероприятия
- Удаление мероприятия
- Фильтрация по названию
- Фильтрация по диапазону дат
- Пагинация результатов
- Ограничение количества мест
- Отслеживание количества свободных мест

### Работа с бронированиями

- Создание бронирования
- Получение статуса бронирования
- Асинхронная обработка бронирований
- Автоматическое подтверждение бронирований
- Автоматическое отклонение бронирований при ошибках
- Возврат мест при отклонении бронирования
- Защита от овербукинга
- Поддержка конкурентных запросов

### Общее

- Swagger UI
- Глобальная обработка ошибок
- Dependency Injection
- Unit-тестирование
- PostgreSQL
- Entity Framework Core

---

## Технологии

- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Npgsql.EntityFrameworkCore.PostgreSQL
- Swagger (Swashbuckle)
- Dependency Injection
- BackgroundService
- Docker Compose
- xUnit
- EF Core InMemory Provider

---

## Архитектура

Проект использует многослойную архитектуру:

- Controllers — HTTP API
- Services — бизнес-логика
- Data Access — Entity Framework Core
- PostgreSQL — постоянное хранилище данных

Основные сущности:

- Event
- Booking

Для доступа к данным используется AppDbContext.

Маппинг сущностей выполняется через Fluent API с использованием IEntityTypeConfiguration<T>.

---

## Структура проекта

```text
EBooking/
├── BackgroundServices/
├── Controllers/
├── DataStore/
│   ├── AppDbContext.cs
│   └── Configurations/
├── DTO/
├── Exceptions/
├── Handlers/
├── Interfaces/
├── Middleware/
├── Models/
├── Services/
├── Program.cs

EBooking.Tests/
├── BookingServiceTests.cs
└── EventsServiceTests.cs
```

---

## База данных

Проект использует PostgreSQL в качестве основного хранилища данных.

Для работы с БД применяется Entity Framework Core.

Схема базы данных автоматически создаётся при запуске приложения:

```csharp
context.Database.EnsureCreated();
```

Создаваемые таблицы:

- events
- bookings

Связь между таблицами:

```text
Booking.EventId -> Event.Id
```

---

## Потокобезопасность

Для защиты от овербукинга используется:

```csharp
private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
```

Это гарантирует корректную обработку конкурентных запросов на бронирование и предотвращает резервирование большего количества мест, чем доступно в мероприятии.

---

## Запуск PostgreSQL

Запуск контейнера:

```bash
docker compose up -d
```

Проверка контейнеров:

```bash
docker ps
```

Пример строки подключения:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ebooking;Username=postgres;Password=postgres"
  }
}
```

---

## Запуск проекта

### 1. Клонирование репозитория

```bash
git clone <repository_url>
cd event-dot-booking
```

### 2. Запуск PostgreSQL

```bash
docker compose up -d
```

### 3. Сборка проекта

```bash
dotnet build
```

### 4. Запуск приложения

```bash
dotnet run --project EBooking
```

---

## Swagger

После запуска приложение будет доступно по адресу:

```text
http://localhost:5000/swagger
```

---

## Тестирование

Запуск тестов:

```bash
dotnet test
```

Тесты используют EF Core InMemory Provider.

Покрываются следующие сценарии:

- CRUD-операции мероприятий
- Создание бронирований
- Валидация бизнес-правил
- Ограничение количества мест
- Защита от овербукинга
- Конкурентные запросы

---

## API Эндпоинты

### Получить список мероприятий

```http
GET /events
```

Параметры:

- title
- from
- to
- page
- pageSize

### Получить мероприятие по идентификатору

```http
GET /events/{id}
```

### Создать мероприятие

```http
POST /events
```

Пример тела запроса:

```json
{
  "title": "ASP.NET Meetup",
  "description": "Introduction to ASP.NET Core",
  "startAt": "2026-04-10T10:00:00",
  "endAt": "2026-04-10T12:00:00",
  "totalSeats": 100
}
```

### Обновить мероприятие

```http
PUT /events/{id}
```

### Удалить мероприятие

```http
DELETE /events/{id}
```

### Создать бронирование

```http
POST /events/{id}/book
```

Возвращает:

```text
202 Accepted
```

Начальный статус бронирования:

```text
Pending
```

### Получить бронирование

```http
GET /bookings/{id}
```

---

## Фоновая обработка бронирований

После создания бронирование получает статус:

```text
Pending
```

Фоновый сервис BookingProcessingBackgroundService периодически проверяет необработанные бронирования и переводит их в:

```text
Confirmed
```

или

```text
Rejected
```

Для корректной работы со scoped-зависимостями используется IServiceScopeFactory.

Каждая операция обработки выполняется в собственном DI Scope и использует собственный экземпляр AppDbContext.

---

## Пример защиты от овербукинга

Событие:

```text
TotalSeats = 3
```

Первые три запроса:

```http
POST /events/{id}/book
```

будут успешно обработаны.

Следующий запрос вернёт:

```text
409 Conflict
```

Количество бронирований никогда не превысит количество доступных мест даже при параллельных запросах.
