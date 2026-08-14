# EBooking API

## Описание

EBooking — REST API для управления мероприятиями и бронированиями, реализованный на ASP.NET Core Web API.

Проект позволяет создавать мероприятия, получать списки событий, выполнять бронирование мест и отслеживать статус бронирований. Для хранения данных используется PostgreSQL.

Проект разделён на три независимых микросервиса:

- Users/Auth — регистрация пользователей, вход и выдача JWT-токенов;
- Events — управление мероприятиями и учёт доступных мест;
- Bookings — создание, получение и отмена бронирований.

Каждый сервис имеет собственную базу данных PostgreSQL, отдельный жизненный цикл и собственные слои Clean Architecture.

Обмен между сервисами Bookings и Events выполняется асинхронно через Apache Kafka. Сервисы не вызывают друг друга напрямую по HTTP.

---

## Возможности

### Аутентификация

API поддерживает JWT Bearer Authentication.

Доступны два типа пользователей:

- User
- Admin

Регистрация выполняется через сервис Users:

```http
POST /auth/register
```

Авторизация:

```http
POST /auth/login
```

После успешного входа сервис Users возвращает JWT-токен, который необходимо передавать в заголовке:

```http
Authorization: Bearer <token>
```

JWT-токен выдаёт только сервис Users.

Сервисы Events и Bookings проверяют тот же токен, используя общие значения:

- Secret
- Issuer
- Audience

Администратор может:

- создавать мероприятия;
- изменять мероприятия;
- удалять мероприятия;
- отменять бронирования пользователей.

Обычный пользователь может:

- просматривать мероприятия;
- создавать собственные бронирования;
- получать информацию о бронированиях;
- отменять только собственные бронирования.

Публичная регистрация создаёт пользователя с ролью:

```text
User
```

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
- Уменьшение доступных мест после получения события из Kafka

Создание, изменение и удаление мероприятий доступны только пользователю с ролью:

```text
Admin
```

### Работа с бронированиями

- Создание бронирования
- Получение бронирования по идентификатору
- Отмена бронирования
- Проверка владельца бронирования
- Асинхронная обработка ожидающих бронирований
- Автоматическое подтверждение бронирований
- Публикация события `BookingConfirmed`
- Ограничение количества активных бронирований пользователя
- Поддержка бронирования нескольких мест

После создания бронирование получает статус:

```text
Pending
```

Фоновый обработчик сервиса Bookings периодически подтверждает ожидающие бронирования и переводит их в статус:

```text
Confirmed
```

После подтверждения сообщение публикуется в Kafka.

### Асинхронное взаимодействие

Сервисы Bookings и Events взаимодействуют через Apache Kafka.

Поток данных:

```text
Bookings API
    |
    | создаёт бронирование со статусом Pending
    v
Bookings PostgreSQL
    |
    | фоновый обработчик подтверждает бронирование
    v
Bookings PostgreSQL
    |
    | публикует BookingConfirmed
    v
Apache Kafka
    |
    | topic: booking-confirmed
    v
Events Consumer
    |
    | уменьшает AvailableSeats
    v
Events PostgreSQL
```

Сервис Bookings:

- не подключается к базе Events;
- не вызывает Events API;
- не изменяет доступные места самостоятельно;
- отвечает только за данные бронирований.

Сервис Events:

- подписывается на топик `booking-confirmed`;
- получает событие `BookingConfirmed`;
- находит мероприятие по `EventId`;
- уменьшает количество доступных мест;
- сохраняет изменения в собственной базе данных.

Такой подход обеспечивает согласованность в конечном счёте — eventual consistency.

### Инфраструктура

- Три ASP.NET Core Web API
- Три базы данных PostgreSQL
- Apache Kafka
- ZooKeeper
- Swagger UI
- JWT Authentication
- Role-based Authorization
- Глобальная обработка ошибок
- Dependency Injection
- Entity Framework Core
- EF Core Migrations
- Repository Pattern
- BackgroundService
- Docker Compose
- Многоступенчатые Dockerfile
- Unit Tests
- Integration Tests
- Testcontainers

---

## Технологии

- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL 16
- Npgsql.EntityFrameworkCore.PostgreSQL
- EF Core Migrations
- Apache Kafka
- Confluent.Kafka
- Redis 7.2
- StackExchange.Redis
- ZooKeeper
- Swagger (Swashbuckle)
- JWT Bearer Authentication
- BCrypt
- Dependency Injection
- BackgroundService
- Docker
- Docker Compose
- Testcontainers
- xUnit

---

## Архитектура

Система построена как набор независимых микросервисов.

Каждый сервис использует принципы **Clean Architecture** и разделён на четыре слоя:

- Domain
- Application
- Infrastructure
- Presentation

Общий контракт Kafka вынесен в отдельный проект `EBooking.Contracts`.

### Users Service

Сервис Users отвечает за:

- регистрацию пользователей;
- нормализацию логина;
- проверку уникальности логина;
- хеширование паролей;
- проверку паролей;
- выдачу JWT-токенов;
- хранение ролей пользователей.

Сервис не содержит данных мероприятий и бронирований.

### Events Service

Сервис Events отвечает за:

- создание мероприятий;
- получение мероприятий;
- фильтрацию и пагинацию;
- изменение мероприятий;
- удаление мероприятий;
- хранение общего и доступного количества мест;
- обработку события `BookingConfirmed`;
- уменьшение количества свободных мест.

Сервис не хранит пользователей и бронирования.

### Bookings Service

Сервис Bookings отвечает за:

- создание бронирований;
- хранение статусов бронирований;
- получение бронирований;
- отмену бронирований;
- проверку владельца бронирования;
- обработку ожидающих бронирований;
- публикацию события `BookingConfirmed`.

Связь с пользователем и мероприятием хранится только по идентификаторам:

```text
UserId
EventId
```

Навигационные свойства и внешние ключи между базами разных сервисов отсутствуют.

### Contracts

Проект `EBooking.Contracts` содержит общий контракт межсервисного сообщения и имя Kafka-топика.

Контракт события:

```csharp
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTimeOffset ConfirmedAt
);
```

Имя топика:

```csharp
public const string BookingConfirmed = "booking-confirmed";
```

Проект Contracts не содержит внутреннюю бизнес-логику сервисов.

### Domain

Содержит предметную область конкретного сервиса:

- доменные сущности;
- перечисления;
- доменные исключения;
- бизнес-правила, не зависящие от инфраструктуры.

Domain не использует Entity Framework Core, ASP.NET Core и другие инфраструктурные библиотеки.

### Application

Содержит прикладную логику:

- use cases;
- DTO;
- интерфейсы репозиториев;
- интерфейсы прикладных сервисов;
- интерфейс Kafka publisher;
- обработчики сообщений.

Application зависит от Domain.

Слои Application сервисов Bookings и Events также используют общий проект Contracts для работы с `BookingConfirmed`.

### Infrastructure

Содержит реализации инфраструктурных компонентов:

- DbContext;
- конфигурации сущностей EF Core;
- репозитории;
- миграции;
- JWT generator;
- password hasher;
- Kafka producer;
- Kafka consumer;
- Kafka topic initializer;
- регистрацию инфраструктурных зависимостей.

### Presentation

Проекты `*.Api` выполняют роль слоя Presentation и содержат:

- контроллеры;
- middleware;
- настройку JWT;
- Swagger;
- `Program.cs`;
- composition root;
- фоновые сервисы.

---

## Структура проекта

```text
src/
├── Blocks/
│   └── EBooking.Contracts/
│       ├── Kafka/
│       │   ├── BookingConfirmed.cs
│       │   └── KafkaTopics.cs
│       └── EBooking.Contracts.csproj
│
└── Services/
    ├── Users/
    │   ├── EBooking.Users.Domain/
    │   ├── EBooking.Users.Application/
    │   ├── EBooking.Users.Infrastructure/
    │   └── EBooking.Users.Api/
    │
    ├── Events/
    │   ├── EBooking.Events.Domain/
    │   ├── EBooking.Events.Application/
    │   ├── EBooking.Events.Infrastructure/
    │   └── EBooking.Events.Api/
    │
    └── Bookings/
        ├── EBooking.Bookings.Domain/
        ├── EBooking.Bookings.Application/
        ├── EBooking.Bookings.Infrastructure/
        └── EBooking.Bookings.Api/

tests/
├── Users/
│   ├── EBooking.Users.Tests/
│   └── EBooking.Users.IntegrationTests/
│
├── Events/
│   ├── EBooking.Events.Tests/
│   └── EBooking.Events.IntegrationTests/
│
└── Bookings/
    ├── EBooking.Bookings.Tests/
    └── EBooking.Bookings.IntegrationTests/

docker-compose.yml
Directory.Packages.props
EBooking.sln
README.md
```

---

## Базы данных

Каждый сервис использует отдельную базу данных PostgreSQL.

### Users Database

База данных:

```text
ebooking_users
```

Основная таблица:

```text
users
```

Хранит:

- идентификатор пользователя;
- логин;
- хеш пароля;
- роль.

### Events Database

База данных:

```text
ebooking_events
```

Основная таблица:

```text
events
```

Хранит:

- название;
- описание;
- дату начала;
- дату окончания;
- общее количество мест;
- количество доступных мест.

### Bookings Database

База данных:

```text
ebooking_bookings
```

Основная таблица:

```text
bookings
```

Хранит:

- идентификатор мероприятия;
- идентификатор пользователя;
- количество мест;
- статус;
- дату создания;
- дату обработки.

Между базами данных нет внешних ключей.

Связи между сервисами представлены только идентификаторами:

```text
bookings.EventId
bookings.UserId
```

---

## Миграции

Каждый сервис имеет собственный DbContext и отдельную миграцию EF Core.

Миграции автоматически применяются при запуске соответствующего API методом:

```csharp
dbContext.Database.MigrateAsync();
```

### Users

Создание новой миграции:

```bash
dotnet ef migrations add MigrationName \
    --project src/Services/Users/EBooking.Users.Infrastructure \
    --startup-project src/Services/Users/EBooking.Users.Api \
    --output-dir Data/Migrations
```

Применение миграций:

```bash
dotnet ef database update \
    --project src/Services/Users/EBooking.Users.Infrastructure \
    --startup-project src/Services/Users/EBooking.Users.Api
```

### Events

Создание новой миграции:

```bash
dotnet ef migrations add MigrationName \
    --project src/Services/Events/EBooking.Events.Infrastructure \
    --startup-project src/Services/Events/EBooking.Events.Api \
    --output-dir Data/Migrations
```

Применение миграций:

```bash
dotnet ef database update \
    --project src/Services/Events/EBooking.Events.Infrastructure \
    --startup-project src/Services/Events/EBooking.Events.Api
```

### Bookings

Создание новой миграции:

```bash
dotnet ef migrations add MigrationName \
    --project src/Services/Bookings/EBooking.Bookings.Infrastructure \
    --startup-project src/Services/Bookings/EBooking.Bookings.Api \
    --output-dir Data/Migrations
```

Применение миграций:

```bash
dotnet ef database update \
    --project src/Services/Bookings/EBooking.Bookings.Infrastructure \
    --startup-project src/Services/Bookings/EBooking.Bookings.Api
```

---

## Apache Kafka

Для асинхронного обмена сообщениями используется Apache Kafka.

Kafka запускается вместе с ZooKeeper через Docker Compose.

### Топик

Используемый топик:

```text
booking-confirmed
```

Топик создаётся сервисом Events при запуске через Kafka Admin Client.

Если топик уже существует, повторное создание пропускается.

### Producer

Kafka producer расположен в Infrastructure сервиса Bookings.

Он:

- зарегистрирован как singleton;
- реализует `IDisposable`;
- сериализует `BookingConfirmed` в JSON;
- использует `EventId` как ключ сообщения;
- публикует сообщение после сохранения статуса бронирования в PostgreSQL.

Порядок выполнения:

```text
1. Booking переводится в Confirmed
2. Изменение сохраняется в Bookings Database
3. BookingConfirmed публикуется в Kafka
```

### Consumer

Kafka consumer расположен в Infrastructure сервиса Events и реализован на базе `BackgroundService`.

Он:

- подписывается на топик `booking-confirmed`;
- использует отдельную consumer group;
- читает сообщения в фоновом режиме;
- создаёт отдельный dependency injection scope для обработки сообщения;
- использует scoped-репозиторий и DbContext;
- вручную фиксирует Kafka offset после обработки;
- логирует ошибочные сообщения;
- не останавливает сервис при отсутствии события или свободных мест.

Для событий одного мероприятия producer использует одинаковый ключ `EventId`, поэтому сообщения одного мероприятия попадают в один partition и сохраняют порядок обработки.

---

## Кеширование Redis

Redis используется сервисом Events.

Кеш реализован по паттерну Cache-Aside. Абстракция кеша находится в слое
Application, реализация на `StackExchange.Redis` --- в Infrastructure.

Используются ключи:

``` text
event:{id}
events:top10
```

### Мероприятие по идентификатору

Для:

``` http
GET /events/{id}
```

сначала выполняется чтение `event:{id}` из Redis.

При попадании в кеш репозиторий не вызывается.

При промахе:

1.  мероприятие загружается из PostgreSQL;
2.  преобразуется в `EventDto`;
3.  сохраняется в Redis с TTL;
4.  возвращается клиенту.

Для отдельного мероприятия используется стратегия инвалидации при
записи.

После успешного изменения или удаления мероприятия:

``` text
1. Изменение сохраняется в PostgreSQL
2. Ключ event:{id} удаляется из Redis
```

Следующий запрос повторно загружает актуальное состояние из PostgreSQL и
прогревает кеш.

Та же инвалидация выполняется после успешной обработки
`BookingConfirmed`, поскольку обработчик изменяет `AvailableSeats`.

### Топ-10 мероприятий

Для:

``` http
GET /events/top
```

кешируется список десяти мероприятий с наибольшим процентом проданных
мест.

Процент рассчитывается как:

``` text
(total_seats - available_seats) / total_seats
```

Ключ:

``` text
events:top10
```

Кеш top-10 не инвалидируется при каждом изменении мероприятия или
бронировании. Он обновляется после истечения TTL.

Для рейтингового агрегата допускается кратковременное устаревание
данных. Это исключает дополнительную инвалидацию общего ключа при каждой
операции изменения доступных мест.

### TTL

Параметры вынесены в конфигурацию:

``` json
{
  "Cache": {
    "EventTtlMinutes": 10,
    "TopEventsTtlMinutes": 5
  }
}
```

Отдельное мероприятие кешируется на 10 минут.

Top-10 кешируется на 5 минут, поскольку рейтинг зависит от изменения
количества доступных мест и должен обновляться чаще.

### Недоступность Redis

Redis используется как необязательный слой кеширования.

Ошибки чтения, записи и удаления кеша логируются и не пробрасываются
клиенту. При недоступности Redis сервис Events продолжает работать с
PostgreSQL.

Соединение `IConnectionMultiplexer` зарегистрировано как singleton.

Для подключения внутри Docker используется:

``` text
redis:6379
```

---

## Фоновая обработка бронирований

После создания бронирование получает статус:

```text
Pending
```

`BookingProcessingBackgroundService` сервиса Bookings запускается вместе с приложением и каждые пять секунд вызывает `IBookingProcessingService`.

При обработке:

1. загружаются идентификаторы ожидающих бронирований;
2. бронирование переводится в статус `Confirmed`;
3. изменение сохраняется в базе Bookings;
4. формируется `BookingConfirmed`;
5. сообщение публикуется в Kafka.

Сервис Events получает сообщение независимо и уменьшает `AvailableSeats`.

---

## Настройка JWT

JWT-токен выдаёт сервис Users.

Параметры JWT должны совпадать во всех трёх сервисах:

```json
{
  "Jwt": {
    "Secret": "development-secret-key-with-at-least-32-bytes",
    "Issuer": "EBooking",
    "Audience": "EBookingClient",
    "LifetimeMinutes": 60
  }
}
```

Для Events и Bookings параметр `LifetimeMinutes` не требуется, поскольку эти сервисы только проверяют токен.

В Docker Compose общие значения передаются переменными окружения:

```text
Jwt__Secret
Jwt__Issuer
Jwt__Audience
```

Значения из репозитория предназначены только для локальной разработки.

Для production-среды секрет необходимо хранить вне исходного кода.

---

## Docker Compose

Вся система запускается одной командой:

```bash
docker compose up --build
```

Docker Compose поднимает:

- Users API
- Events API
- Bookings API
- Users PostgreSQL
- Events PostgreSQL
- Bookings PostgreSQL
- ZooKeeper
- Kafka
- Redis

### Порты

| Компонент | Адрес |
|---|---|
| Users API | `http://localhost:5001` |
| Events API | `http://localhost:5002` |
| Bookings API | `http://localhost:5003` |
| Users Swagger | `http://localhost:5001/swagger` |
| Events Swagger | `http://localhost:5002/swagger` |
| Bookings Swagger | `http://localhost:5003/swagger` |

PostgreSQL, Kafka и ZooKeeper доступны только внутри Docker-сети и не публикуют порты на хост.

### Запуск системы

```bash
docker compose up --build -d
```

### Проверка контейнеров

```bash
docker compose ps
```

### Просмотр логов

```bash
docker compose logs -f
```

Логи только API:

```bash
docker compose logs -f users-api events-api bookings-api
```

Логи Kafka-взаимодействия:

```bash
docker compose logs --tail=100 bookings-api events-api
```

### Остановка системы

```bash
docker compose down
```

Остановка с удалением томов и данных PostgreSQL:

```bash
docker compose down -v
```

### Полная пересборка

```bash
docker compose build --no-cache

docker compose up -d --force-recreate
```

---

## Запуск проекта

### Клонирование репозитория

```bash
git clone <repository_url>
cd event-dot-booking
```

### Сборка

```bash
dotnet build
```

### Запуск через Docker Compose

Рекомендуемый способ запуска всей системы:

```bash
docker compose up --build -d
```

### Локальный запуск отдельных сервисов

Для локального запуска вне Docker необходимы:

- PostgreSQL;
- Kafka;
- Redis для работы кеша;
- корректные строки подключения;
- одинаковые параметры JWT.

Запуск Users:

```bash
dotnet run \
    --project src/Services/Users/EBooking.Users.Api
```

Запуск Events:

```bash
dotnet run \
    --project src/Services/Events/EBooking.Events.Api
```

Запуск Bookings:

```bash
dotnet run \
    --project src/Services/Bookings/EBooking.Bookings.Api
```

---

## Swagger

Swagger доступен отдельно для каждого сервиса.

### Users

```text
http://localhost:5001/swagger
```

### Events

```text
http://localhost:5002/swagger
```

### Bookings

```text
http://localhost:5003/swagger
```

Events и Bookings настроены для работы с JWT через кнопку:

```text
Authorize
```

Для проверки защищённых методов:

1. выполнить `POST /auth/login` в Users API;
2. скопировать полученный JWT;
3. открыть Swagger сервиса Events или Bookings;
4. нажать кнопку `Authorize`;
5. вставить JWT-токен;
6. выполнить защищённый запрос.

Для схемы Bearer Authentication Swagger принимает JWT без ручного добавления заголовка `Authorization`.

---

## API эндпоинты

## Users API

Базовый адрес:

```text
http://localhost:5001
```

### Регистрация пользователя

```http
POST /auth/register
```

Пример запроса:

```json
{
  "login": "user",
  "password": "Password123"
}
```

При успешной регистрации возвращается:

```text
204 No Content
```

Новый пользователь получает роль:

```text
User
```

### Аутентификация пользователя

```http
POST /auth/login
```

Пример запроса:

```json
{
  "login": "user",
  "password": "Password123"
}
```

Ответ содержит:

- данные пользователя;
- роль;
- JWT-токен.

---

## Events API

Базовый адрес:

```text
http://localhost:5002
```

### Получить список мероприятий

```http
GET /events
```

Параметры:

- `title`
- `from`
- `to`
- `page`
- `pageSize`

Пример:

```http
GET /events?title=conference&page=1&pageSize=10
```

Эндпоинт доступен без аутентификации.

### Получить мероприятие по идентификатору

```http
GET /events/{id}
```

Эндпоинт доступен без аутентификации. Результат кешируется в Redis по ключу `event:{id}`.

### Получить топ-10 мероприятий

``` http
GET /events/top
```

Возвращает до 10 мероприятий с наибольшим процентом проданных мест.

Эндпоинт доступен без аутентификации.

Результат кешируется в Redis по ключу `events:top10` и обновляется по
TTL.

### Создать мероприятие

```http
POST /events
```

Требуемая роль:

```text
Admin
```

Пример запроса:

```json
{
  "title": "ASP.NET Core Conference",
  "description": "Conference for .NET developers",
  "startAt": "2026-08-10T10:00:00Z",
  "endAt": "2026-08-10T18:00:00Z",
  "totalSeats": 100
}
```

### Обновить мероприятие

```http
PUT /events/{id}
```

Требуемая роль:

```text
Admin
```

Пример запроса:

```json
{
  "title": "Updated ASP.NET Core Conference",
  "description": "Updated conference description",
  "startAt": "2026-08-10T11:00:00Z",
  "endAt": "2026-08-10T19:00:00Z"
}
```

### Удалить мероприятие

```http
DELETE /events/{id}
```

Требуемая роль:

```text
Admin
```

---

## Bookings API

Базовый адрес:

```text
http://localhost:5003
```

Все эндпоинты бронирований требуют JWT-аутентификации.

### Создать бронирование

```http
POST /api/bookings
```

Пример запроса:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "seatsCount": 1
}
```

Идентификатор пользователя читается из JWT claims.

Начальный статус:

```text
Pending
```

При успешном создании возвращается:

```text
201 Created
```

### Получить бронирование

```http
GET /api/bookings/{id}
```

### Отменить бронирование

```http
DELETE /api/bookings/{id}
```

Обычный пользователь может отменить только собственное бронирование.

Пользователь с ролью `Admin` может отменить бронирование другого пользователя.

---

## Создание администратора для локальной проверки

Публичная регистрация создаёт только обычного пользователя с ролью `User`.

Для локальной проверки административных эндпоинтов можно:

1. зарегистрировать пользователя через `POST /auth/register`;
2. изменить его роль непосредственно в базе Users;
3. повторно выполнить вход и получить новый JWT с ролью `Admin`.

При использовании Docker Compose:

```bash
docker compose exec users-db \
    psql -U postgres -d ebooking_users
```

После подключения выполните:

```sql
UPDATE users
SET "Role" = 'Admin'
WHERE "Login" = 'user';
```

Логин в запросе должен совпадать с логином зарегистрированного пользователя. Роль хранится в строковом виде.

После изменения роли старый JWT использовать нельзя. Необходимо снова выполнить:

```http
POST /auth/login
```

---

## Проверка полного сценария

### 1. Запустить систему

```bash
docker compose up --build -d
```

### 2. Проверить состояние контейнеров

```bash
docker compose ps
```

Все API и инфраструктурные контейнеры должны иметь состояние `Up`, а базы данных, Kafka и ZooKeeper — пройти healthcheck.

### 3. Зарегистрировать пользователя

```http
POST http://localhost:5001/auth/register
```

### 4. Выполнить вход

```http
POST http://localhost:5001/auth/login
```

Сохранить полученный JWT.

### 5. Получить JWT администратора

Создать или назначить пользователю роль `Admin`, затем повторно выполнить вход.

### 6. Создать мероприятие

```http
POST http://localhost:5002/events
Authorization: Bearer <admin_token>
```

Сохранить:

- `Id` мероприятия;
- исходное значение `AvailableSeats`.

### 7. Создать бронирование

```http
POST http://localhost:5003/api/bookings
Authorization: Bearer <user_token>
```

Пример тела:

```json
{
  "eventId": "<event_id>",
  "seatsCount": 2
}
```

### 8. Дождаться обработки

Фоновый обработчик Bookings запускается каждые пять секунд.

### 9. Проверить бронирование

```http
GET http://localhost:5003/api/bookings/{booking_id}
Authorization: Bearer <user_token>
```

Ожидаемый статус:

```text
Confirmed
```

### 10. Проверить мероприятие

```http
GET http://localhost:5002/events/{event_id}
```

Значение `AvailableSeats` должно уменьшиться на количество мест из бронирования.

### 11. Проверить логи

```bash
docker compose logs --tail=100 bookings-api events-api
```

В логах должны присутствовать сообщения о:

- подтверждении бронирования;
- публикации `BookingConfirmed`;
- получении события сервисом Events;
- уменьшении количества доступных мест.

---

## Проверка авторизации

Минимальные сценарии проверки:

```text
POST /events без JWT              -> 401 Unauthorized
POST /events с ролью User         -> 403 Forbidden
POST /events с ролью Admin        -> 201 Created
POST /api/bookings без JWT        -> 401 Unauthorized
POST /api/bookings с JWT          -> 201 Created
DELETE чужого бронирования User   -> 403 Forbidden
```

---

## Тестирование

Проект содержит отдельные unit- и integration-тесты для каждого сервиса.

### Users Unit Tests

```bash
dotnet test \
    tests/Users/EBooking.Users.Tests
```

Проверяются:

- AuthenticationService;
- PasswordHasher;
- JwtTokenGenerator;
- регистрация;
- вход;
- проверка паролей;
- обработка неверных учётных данных.

### Users Integration Tests

```bash
dotnet test \
    tests/Users/EBooking.Users.IntegrationTests
```

Проверяются:

- применение миграции;
- UserRepository;
- регистрация через API;
- вход через API;
- работа с PostgreSQL.

### Events Unit Tests

```bash
dotnet test \
    tests/Events/EBooking.Events.Tests
```

Проверяются:

- доменные правила Event;
- EventsService;
- создание и изменение мероприятий;
- работа с доступными местами.

### Events Integration Tests

```bash
dotnet test \
    tests/Events/EBooking.Events.IntegrationTests
```

Проверяются:

- EventRepository;
- работа с PostgreSQL;
- фильтрация;
- пагинация;
- сохранение мероприятий.

### Bookings Unit Tests

```bash
dotnet test \
    tests/Bookings/EBooking.Bookings.Tests
```

Проверяются:

- доменные правила Booking;
- BookingService;
- BookingProcessingService;
- ограничение активных бронирований;
- отмена бронирований;
- формирование `BookingConfirmed`.

### Bookings Integration Tests

```bash
dotnet test \
    tests/Bookings/EBooking.Bookings.IntegrationTests
```

Проверяются:

- применение миграции;
- BookingRepository;
- работа с PostgreSQL;
- сохранение и получение бронирований;
- изменение статусов.

### Запуск всех тестов

```bash
dotnet test
```

Для запуска интеграционных тестов требуется запущенный Docker.

Интеграционные тесты автоматически поднимают PostgreSQL через Testcontainers.

---

## Сборка и финальная проверка

Проверка solution:

```bash
dotnet sln EBooking.sln list
```

Сборка:

```bash
dotnet build
```

Запуск всех тестов:

```bash
dotnet test
```

Запуск всей системы:

```bash
docker compose up --build -d
```

Проверка контейнеров:

```bash
docker compose ps
```

Проверка Swagger:

```text
http://localhost:5001/swagger
http://localhost:5002/swagger
http://localhost:5003/swagger
```

---

## Особенности согласованности

Система использует eventual consistency.

После подтверждения бронирования:

- статус бронирования уже сохранён в Bookings Database;
- событие публикуется в Kafka;
- Events обрабатывает сообщение независимо;
- количество доступных мест изменяется не в рамках исходного HTTP-запроса, а позднее.

Поэтому сразу после создания бронирования Events API может кратковременно возвращать прежнее значение `AvailableSeats`.

Это ожидаемое поведение событийно-ориентированной архитектуры.

---

## Ограничения текущей реализации

Сервисы намеренно не выполняют прямые синхронные HTTP-вызовы друг к другу.

Из этого следует:

- Bookings не проверяет существование мероприятия при создании бронирования;
- Bookings не знает текущее количество мест в Events;
- недостаток мест обрабатывается сервисом Events после получения сообщения;
- данные сервисов согласуются асинхронно.

Kafka обеспечивает повторную доставку сообщений в некоторых сценариях. Для полноценной production-идемпотентности consumer может быть расширен хранилищем обработанных `BookingId`.

---

## Безопасность

Текущие значения паролей PostgreSQL и JWT secret предназначены только для локальной разработки.

Для production-среды необходимо:

- использовать отдельные секреты;
- хранить секреты в переменных окружения или secret storage;
- не публиковать реальные пароли в репозитории;
- использовать TLS;
- ограничить сетевой доступ к PostgreSQL и Kafka;
- настроить резервное копирование баз данных;
- настроить мониторинг Kafka consumer lag;
- использовать устойчивую конфигурацию Kafka с несколькими broker-узлами.
