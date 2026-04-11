# EBooking API

## Описание

EBooking — REST API-сервис для управления мероприятиями, реализованный на ASP.NET Core Web API.

Проект представляет собой каркас backend-приложения с CRUD-операциями.

---

## Возможности

* Создание мероприятий
* Получение списка мероприятий
* Получение мероприятия по ID
* Обновление мероприятия
* Удаление мероприятия
* Валидация входных данных
* Swagger UI для тестирования API
* Глобальная обработка ошибок через middleware
* Фильтрация мероприятий по названию и датам
* Пагинация результатов
* Unit-тесты для бизнес-логики сервиса

---

## Технологии

* C#
* .NET 8+
* ASP.NET Core Web API
* Swagger (Swashbuckle)
* Dependency Injection (DI)
* xUnit
---

## Структура проекта

```
EBooking/
├── Controllers/     # Контроллеры API
├── DTO/             # DTO для запросов и ответов
├── Exceptions/      # Пользовательские исключения
├── Handlers/        # Модели ответов и ошибок
├── Interfaces/      # Интерфейсы сервисов
├── Middleware/      # Глобальный middleware обработки ошибок
├── Models/          # Доменные модели
├── Services/        # Бизнес-логика
├── Program.cs       # Конфигурация приложения
└── README.md

EBooking.Tests/
└── EventsServiceTests.cs  # Unit-тесты для сервиса
```

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
        "id": 1,
        "title": "ASP.NET Basic",
        "description": "Introduction to ASP.NET Core",
        "startAt": "2026-04-10T10:00:00",
        "endAt": "2026-04-10T12:00:00"
      },
      {
        "id": 2,
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
  "endAt": "2026-03-25T12:00:00"
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

---

## Ограничения

* Данные не сохраняются между перезапусками
* Нет авторизации
