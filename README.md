# EBooking API

## Описание

EBooking — REST API-сервис для управления мероприятиями, реализованный на ASP.NET Core Web API.

Проект представляет собой каркас backend-приложения с CRUD-операциями.

---

## Возможности

### Работа с мероприятиями

- Создание мероприятий
- Получение списка мероприятий
- Получение мероприятия по ID
- Обновление мероприятия
- Удаление мероприятия
- Фильтрация по названию и датам
- Пагинация
- Ограничение количества мест
- Защита от овербукинга

### Работа с бронированиями

- Создание брони (202 Accepted)
- Получение статуса брони
- Асинхронная обработка бронирований
- Фоновый сервис обработки
- Параллельная обработка бронирований
- Автоматическое подтверждение брони
- Автоматическое отклонение брони при ошибках
- Возврат мест при отклонении брони
- 409 Conflict при отсутствии свободных мест

### Общее

- Валидация входных данных
- Swagger UI
- Глобальная обработка ошибок
- Unit-тесты бизнес-логики

---

## Технологии

* C#
* .NET 8+
* ASP.NET Core Web API
* Swagger (Swashbuckle)
* Dependency Injection (DI)
* BackgroundService
* xUnit


---

## Структура проекта

```
EBooking/
├── Controllers/
├── DTO/
├── Exceptions/
├── Handlers/
├── Interfaces/
├── Middleware/
├── Models/
├── Services/
├── DataStore/
├── BackgroundServices/
├── Program.cs

EBooking.Tests/
├── EventsServiceTests.cs
└── BookingServiceTests.cs

README.md 
```

## Потокобезопасность

В проекте реализована защита от овербукинга при конкурентных запросах.

### BookingService

Для защиты критической секции используется:

```csharp
lock (_bookingLock)

---

## Запуск проекта

### 1. Клонирование репозитория

```bash
git clone <repo_url>
cd event-dot-booking
```

### 2. Сборка проекта

```bash
dotnet build
```

### 3. Запуск

```bash
dotnet run --project EBooking
```

---

## Swagger

После запуска приложение будет доступно по адресу:

```
http://localhost:5000/swagger
```

## Tests

Для запуска unit-тестов выполните команду:

```
dotnet test
```

---

## API эндпоинты

### 🔹 Получить все события

```
GET /events
```

Поддерживаемые query-параметры:

* title — поиск по названию, частичное совпадение, без учёта регистра;
* from — вернуть события, начинающиеся не раньше указанной даты;
* to — вернуть события, заканчивающиеся не позже указанной даты;
* page — номер страницы, по умолчанию 1;
* pageSize — размер страницы, по умолчанию 10.

```
GET /api/events?title=asp.net&from=2026-04-01T00:00:00&to=2026-04-30T23:59:59&page=1&pageSize=5
```

Пример ответа:

```json
{
  "status": true,
  "dateTime": "2026-04-11T10:30:00Z",
  "message": "Events returned successfully",
  "data": {
    "totalCount": 2,
    "page": 1,
    "pageSize": 5,
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "title": "ASP.NET Basic",
        "description": "Introduction to ASP.NET Core",
        "startAt": "2026-04-10T10:00:00",
        "endAt": "2026-04-10T12:00:00"
      },
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
        "title": "ASP.NET Advanced",
        "description": "Advanced topics",
        "startAt": "2026-04-20T10:00:00",
        "endAt": "2026-04-20T12:00:00"
      }
    ]
  }
}
```

---

### 🔹 Получить событие по ID

```
GET /events/{id}
```

**Ответ:**

* 200 OK — если найдено
* 404 Not Found — если нет

---

### 🔹 Создать событие

```
POST /events
```

**Body:**

```json
{
  "title": "Event name",
  "description": "Optional",
  "startAt": "2026-03-25T10:00:00",
  "endAt": "2026-03-25T12:00:00",
  "totalSeats": 100
}
```

**Ответ:**

* 201 Created — успешно
* 400 Bad Request — ошибка валидации

---

### 🔹 Обновить событие

```
PUT /events/{id}
```

---

### 🔹 Удалить событие

```
DELETE /events/{id}
```

### 🔹 Создать бронь

```
POST /events/{id}/book
```

Возвращает 202 Accepted. Обработка выполняется асинхронно
В заголовке Location возвращается ссылка на бронь
Ответ
{
  "status": true,
  "message": "Booking created successfully",
  "data": {
    "id": "guid",
    "eventId": "guid",
    "status": "Pending",
    "createdAt": "2026-04-10T10:00:00Z",
    "processedAt": null
  }
}


### 🔹 Получить бронь

```
GET /bookings/{id}
```

## Фоновая обработка

В проекте реализован BackgroundService, который:

* Периодически ищет брони со статусом `Pending`
* Имитирует обращение к внешней системе (`Task.Delay`)
* Переводит бронь в `Confirmed`
* Заполняет поле `ProcessedAt`

---

## Ограничения

* Данные не сохраняются между перезапусками
* Нет авторизации

## Пример защиты от овербукинга

Событие:
* TotalSeats = 3

Запросы:
* 3 первых POST /events/{id}/book -> 202 Accepted
* 4-й запрос -> 409 Conflict

Сервис гарантирует, что количество подтверждённых бронирований не превысит количество мест даже при параллельных запросах.