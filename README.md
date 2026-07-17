# EBooking API

## Описание

EBooking — REST API для управления мероприятиями и бронированиями, реализованный на ASP.NET Core Web API.

Проект позволяет создавать мероприятия, получать списки событий, выполнять бронирование мест и отслеживать статус бронирований. Для хранения данных используется PostgreSQL.

---

## Возможности

### Аутентификация

API поддерживает JWT Bearer Authentication.

Доступны два типа пользователей:

- User
- Admin

Регистрация выполняется через:

POST /auth/register

Авторизация:

POST /auth/login

После успешного входа сервер возвращает JWT-токен, который необходимо передавать в заголовке:

Authorization: Bearer <token>

Администратор может:

- создавать мероприятия;
- изменять мероприятия;
- удалять мероприятия.

Обычный пользователь может:

- просматривать мероприятия;
- создавать собственные бронирования;
- отменять только собственные бронирования.

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
- Автоматическое отклонение бронирований
- Возврат мест при отклонении бронирования
- Защита от овербукинга
- Поддержка конкурентных запросов

### Инфраструктура

- Swagger UI
- JWT Authentication
- Role-based Authorization
- Глобальная обработка ошибок
- Dependency Injection
- Entity Framework Core
- PostgreSQL
- EF Core Migrations
- Repository Pattern
- Unit Tests
- Integration Tests
- Testcontainers

---

## Технологии

- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Npgsql.EntityFrameworkCore.PostgreSQL
- EF Core Migrations
- Swagger (Swashbuckle)
- Dependency Injection
- BackgroundService
- Docker Compose
- Testcontainers
- xUnit

---

## Архитектура

Проект построен по принципам **Clean Architecture** и разделён на четыре независимых слоя.

### Domain

Содержит предметную область приложения:

- доменные сущности (`Event`, `Booking`);
- доменные исключения;
- бизнес-правила, не зависящие от инфраструктуры.

Данный слой не использует Entity Framework Core, ASP.NET Core и другие внешние библиотеки.

### Application

Содержит бизнес-логику приложения:

- сервисы (use cases);
- DTO;
- интерфейсы репозиториев (порты);
- интерфейсы прикладных сервисов.

Слой зависит только от `Domain` и не знает о способе хранения данных.

### Infrastructure

Содержит реализации инфраструктурных компонентов:

- `AppDbContext`;
- репозитории Entity Framework Core;
- конфигурации моделей;
- миграции базы данных;
- регистрацию инфраструктурных зависимостей.

### Presentation

В текущем решении проект `EBooking` выполняет роль слоя Presentation и содержит Web API:

- контроллеры;
- middleware;
- обработчики ошибок;
- `Program.cs` (composition root);
- `BackgroundService`.

Presentation обращается только к сервисам слоя Application.

---

## Структура проекта

```text
EBooking.Domain/
├── Entities/
└── Exceptions/

EBooking.Application/
├── DTO/
├── Interfaces/
├── Services/
└── DependencyInjection.cs

EBooking.Infrastructure/
├── DataStore/
│   └── Configurations/
├── Migrations/
├── Repositories/
└── DependencyInjection.cs

EBooking/  # Presentation layer
├── BackgroundServices/
├── Controllers/
├── Handlers/
├── Middlewares/
└── Program.cs

EBooking.Tests/

EBooking.IntegrationTests/
```

---

## База данных

Проект использует PostgreSQL в качестве основного хранилища данных.

Схема базы данных управляется миграциями EF Core.

Создаваемые таблицы:

- events
- bookings
- users

Связь между таблицами:

```text
bookings.event_id -> events.id
bookings.user_id  -> users.id
```

---

## Миграции

Создание новой миграции:

```bash
dotnet ef migrations add MigrationName \
    --project EBooking.Infrastructure \
    --startup-project EBooking
```

Применение миграций:

```bash
dotnet ef database update \
    --project EBooking.Infrastructure \
    --startup-project EBooking
```

Удаление последней миграции:

```bash
dotnet ef migrations remove \
    --project EBooking.Infrastructure \
    --startup-project EBooking
```

При запуске приложения миграции автоматически применяются методом:

```csharp
db.Database.Migrate();
```

---

## Потокобезопасность

Для защиты от овербукинга используется:

```csharp
private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
```

---

## Запуск PostgreSQL

```bash
docker compose up -d
```

Проверка контейнеров:

```bash
docker ps
```

---

## Запуск проекта

### Клонирование репозитория

```bash
git clone <repository_url>
cd event-dot-booking
```

### Запуск PostgreSQL

```bash
docker compose up -d
```

### Сборка

```bash
dotnet build
```

### Запуск приложения

```bash
dotnet run --project EBooking
```

---

## Swagger

После запуска приложение предоставляет Swagger UI.

Для проверки защищённых методов:

1. выполнить POST /auth/login;
2. скопировать полученный JWT;
3. нажать кнопку Authorize;
4. вставить токен.

После этого защищённые эндпоинты будут выполняться от имени авторизованного пользователя.

---

## Тестирование

### Unit Tests

```bash
dotnet test EBooking.Tests
```

### Integration Tests

```bash
dotnet test EBooking.IntegrationTests
```

Для запуска интеграционных тестов требуется запущенный Docker.

Интеграционные тесты автоматически поднимают PostgreSQL через Testcontainers.

Покрываются:

- Применение миграций
- EventRepository
- BookingRepository
- Работа с PostgreSQL
- Фильтрация
- Пагинация
- Изменение статусов бронирований

### Запуск всех тестов

```bash
dotnet test
```

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

Начальный статус:

```text
Pending
```

### Получить бронирование

```http
GET /bookings/{id}
```

### Регистрация пользователя
```http
POST /auth/register
```

### Аутентификация пользователя
```http
POST /auth/login
```

---

## Фоновая обработка бронирований

После создания бронирование получает статус:

```text
Pending
```

Фоновый сервис `BookingProcessingBackgroundService` периодически вызывает прикладной сервис `IBookingProcessingService`, который обрабатывает необработанные бронирования и переводит их в состояния:

```text
Confirmed
```

или

```text
Rejected
```

Вся бизнес-логика обработки бронирований находится в слое Application.
