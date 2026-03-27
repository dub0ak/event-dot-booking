# EBooking API

## Описание

EBooking — REST API-сервис для управления мероприятиями, реализованный на ASP.NET Core Web API.

Проект представляет собой базовый каркас backend-приложения с CRUD-операциями.

---

## Возможности

* Создание мероприятий
* Получение списка мероприятий
* Получение мероприятия по ID
* Обновление мероприятия
* Удаление мероприятия
* Валидация входных данных
* Swagger UI для тестирования API

---

## Технологии

* C#
* .NET 8 / .NET 9
* ASP.NET Core Web API
* Swagger (Swashbuckle)
* Dependency Injection (DI)

---

## Структура проекта

```
EBooking/
│
├── Controllers/      # HTTP-эндпоинты
├── Services/         # Бизнес-логика
├── Interfaces/       # Интерфейсы сервисов
├── Models/           # Доменная модель
├── Handlers/         # Формат API-ответов
└── Program.cs        # Конфигурация приложения
```

---

## Запуск проекта

### 1. Клонирование репозитория

```bash
git clone <repo_url>
cd EBooking
```

### 2. Сборка проекта

```bash
dotnet build
```

### 3. Запуск

```bash
dotnet run
```

---

## Swagger

После запуска приложение будет доступно по адресу:

```
http://localhost:5000/swagger
```

---

## API эндпоинты

### 🔹 Получить все события

```
GET /api/events
```

---

### 🔹 Получить событие по ID

```
GET /api/events/{id}
```

**Ответ:**

* 200 OK — если найдено
* 404 Not Found — если нет

---

### 🔹 Создать событие

```
POST /api/events
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
PUT /api/events/{id}
```

---

### 🔹 Удалить событие

```
DELETE /api/events/{id}
```

---

## Особенности

* Данные хранятся в памяти (`List<Event>`)
* Бизнес-логика вынесена в сервис (`EventsService`)
* Используется Dependency Injection
* Единый формат ответа API (`ApiResult`)

---

## Ограничения

* Данные не сохраняются между перезапусками
* Нет авторизации
