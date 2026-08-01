# Eventum

Eventum — система для управления событиями и бронированиями. Проект был разделён на три независимых ASP.NET Core Web API сервиса и один общий проект с контрактами сообщений.

## Требования

Для запуска проекта необходимы:

- установленный Docker;
- доступные порты `5001`, `5002`, `5003`, `5433`, `5434`, `5435`, `6379`, `9092`;
- .NET SDK 10 для локальной сборки и запуска без контейнеров.

## Состав системы

| Сервис | Проект | Порт | База данных | Ответственность |
| --- | --- | --- | --- | --- |
| Users | `Eventum.Users.WebApi` | `5001` | `users` | Регистрация, вход, хеширование пароля, выдача JWT |
| Events | `Eventum.Events.WebApi` | `5002` | `events` | CRUD событий и учёт доступных мест |
| Bookings | `Eventum.Bookings.WebApi` | `5003` | `bookings` | Создание, отмена и подтверждение броней |
| Contracts | `Eventum.Shared.Contracts` | - | - | Общие Kafka-контракты |

Каждый сервис построен по принципам чистой архитектуры:

- `Domain` — сущности и доменные правила;
- `Application` — use cases, DTO, интерфейсы сервисов и репозиториев;
- `Infrastructure` — EF Core, репозитории, Kafka, фоновые сервисы;
- `WebApi` — контроллеры, JWT, DI-конфигурация и точка входа.

Сервисы не используют общую схему БД. Связи между сервисами хранятся только по идентификаторам, например `Booking.EventId` и `Booking.UserId`. Прямых HTTP-вызовов между сервисами нет.

## Аутентификация и авторизация

JWT-токен выдаёт только сервис Users. Сервисы Events и Bookings проверяют токен по общим значениям:

- `JwtSettings:Secret`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`

Ролевая модель:

| Роль | Уровень доступа |
| --- | --- |
| `Admin` | управление событиями |
| `User` | создание, просмотр и отмена своих броней |

Публичные эндпоинты:

- `POST /auth/register` в Users — регистрация пользователя;
- `POST /auth/login` в Users — получение JWT-токена;
- `GET /events` в Events — список событий;
- `GET /events/{id}` в Events — событие по идентификатору;
- `GET /events/top` в Events — топ-10 событий по проценту проданных мест.

Эндпоинты, требующие JWT:

- `POST /events/{eventId}/book` в Bookings — создание брони;
- `GET /bookings/{id}` в Bookings — получение брони;
- `DELETE /bookings/{id}` в Bookings — отмена брони.

Эндпоинты, требующие роль `Admin`:

- `POST /events` в Events — создание события;
- `PUT /events/{id}` в Events — обновление события;
- `DELETE /events/{id}` в Events — удаление события.

## Kafka и контракт BookingConfirmed

Общий контракт находится в проекте `Eventum.Shared.Contracts`.

- Имя топика: `BookingTopics.BookingConfirmed`
- Значение топика: `booking-confirmed`
- Контракт сообщения: `BookingConfirmed`
- Поля сообщения: `BookingId`, `EventId`, `UserId`, `Seats`, `ConfirmedAt`

Поток данных:

1. Bookings создаёт бронь со статусом `Pending` в своей базе.
2. Фоновый сервис Bookings находит pending-брони.
3. Bookings подтверждает бронь и сначала сохраняет статус `Confirmed` в свою БД.
4. Bookings публикует `BookingConfirmed` в Kafka.
5. Ключ Kafka-сообщения — `EventId`, чтобы сообщения по одному событию обрабатывались последовательно.
6. Events подписывается на `booking-confirmed`.
7. Events получает сообщение, находит событие в своей БД и уменьшает `AvailableSeats`.
8. Если событие не найдено или мест недостаточно, Events логирует проблему и пропускает сообщение, не останавливая подписчик.

Сервис Bookings не уменьшает места и не обращается к Events напрямую. Изменение доступных мест происходит только через Kafka.

## Redis и стратегия кеширования

Redis подключён к сервису Events через `StackExchange.Redis`. Соединение `IConnectionMultiplexer` регистрируется в DI как singleton. Слой Application зависит только от интерфейса `ICacheService`; конкретная Redis-реализация находится в Infrastructure.

В сервисе событий используется Cache-Aside для двух read-сценариев:

- `GET /events/{id}` кешируется по ключу `event:{id}`;
- `GET /events/top` кешируется по ключу `events:top10`.

TTL вынесены в конфигурацию `Redis`. Для отдельного события выбран TTL 300 секунд: карточка события читается часто, но при изменениях должна быстро становиться актуальной. Для топ-10 выбран TTL 60 секунд: это рейтинговый агрегат, который может немного устаревать, но должен чаще обновляться из-за влияния новых бронирований на процент продаж.

Для отдельного события выбрана стратегия инвалидации при записи. После успешного сохранения изменений в базу ключ `event:{id}` удаляется при `PUT /events/{id}`, `DELETE /events/{id}` и при обработке Kafka-сообщения `BookingConfirmed`, которое уменьшает `AvailableSeats`. Порядок операций намеренно такой: сначала запись в БД, затем изменение кеша. Если процесс оборвётся между этими шагами, база останется источником истины, а кеш обновится при следующем промахе.

Кеш топ-10 живёт только по TTL. Явная инвалидация при каждом бронировании не используется, потому что список является агрегатом и небольшое устаревание для рейтинга некритично.

Если Redis недоступен, Redis-адаптер логирует ошибки и не пробрасывает их клиенту. Чтение продолжает работать через PostgreSQL, а операции записи сначала сохраняют данные в базу. Для старта без доступного Redis соединение создаётся с `AbortOnConnectFail=false`.

## Запуск через Docker

Полный запуск системы описан в `docker-compose.yml`.

```bash
docker compose -f docker-compose.yml up --build -d
```

После запуска доступны:

- Users API: `http://localhost:5001`
- Events API: `http://localhost:5002`
- Bookings API: `http://localhost:5003`
- Kafka: `localhost:9092`
- Users DB: `localhost:5433`
- Events DB: `localhost:5434`
- Bookings DB: `localhost:5435`
- Redis: `localhost:6379`

Каждый API-сервис применяет свои EF Core миграции при старте.

## Локальный запуск API без контейнеров сервисов

Можно поднять только инфраструктуру, а API запустить через `dotnet run`.

```bash
docker compose -f docker-compose.yml up zookeeper kafka users-db events-db bookings-db redis
dotnet run --project Eventum.Users.WebApi/Eventum.Users.WebApi.csproj
dotnet run --project Eventum.Events.WebApi/Eventum.Events.WebApi.csproj
dotnet run --project Eventum.Bookings.WebApi/Eventum.Bookings.WebApi.csproj
```

Команды `dotnet run` нужно запускать в отдельных терминалах.

## Сборка

```bash
dotnet restore Eventum.slnx
dotnet build Eventum.slnx --no-restore
```

## Документация API

### 1. Регистрация пользователя

- **Сервис:** Users
- **Метод:** `POST`
- **URL:** `http://localhost:5001/auth/register`
- **Тело запроса:** `RegisterRequest`
- **Ответ:** `204 No Content`
- **Ответ:** `400 Bad Request`, если пользователь уже существует

Пример:

```http
POST /auth/register
Content-Type: application/json

{
  "login": "user",
  "password": "password",
  "role": "User"
}
```

Для администратора укажите `"role": "Admin"`.

### 2. Получение JWT-токена

- **Сервис:** Users
- **Метод:** `POST`
- **URL:** `http://localhost:5001/auth/login`
- **Тело запроса:** `LoginRequest`
- **Ответ:** `200 OK` — `AuthResponse`
- **Ответ:** `401 Unauthorized`, если логин или пароль неверны

Пример:

```http
POST /auth/login
Content-Type: application/json

{
  "login": "user",
  "password": "password"
}
```

Пример ответа:

```json
{
  "token": "jwt-token"
}
```

### 3. Получение списка событий с пагинацией

- **Сервис:** Events
- **Метод:** `GET`
- **URL:** `http://localhost:5002/events`
- **Тело запроса:** нет
- **Ответ:** `200 OK` — `PaginatedResult<Event>`

Query-параметры:

- `title` — поиск по названию события;
- `from` — вернуть события, которые начинаются не раньше указанной даты;
- `to` — вернуть события, которые заканчиваются не позже указанной даты;
- `page` — номер страницы, по умолчанию `1`;
- `pageSize` — количество элементов на странице, по умолчанию `10`.

Пример:

```http
GET /events?title=meet&from=2026-01-01&page=1&pageSize=5
```

Пример ответа:

```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "count": 1,
  "items": [
    {
      "id": "27585d89-5ef9-40ac-b282-520f27369741",
      "title": "Conference",
      "description": "Demo event",
      "startAt": "2026-08-01T10:00:00Z",
      "endAt": "2026-08-01T12:00:00Z",
      "totalSeats": 10,
      "availableSeats": 10
    }
  ]
}
```

### 4. Получение события по ID

- **Сервис:** Events
- **Метод:** `GET`
- **URL:** `http://localhost:5002/events/{id}`
- **Тело запроса:** нет
- **Ответ:** `200 OK` — `EventResponseDto`
- **Ответ:** `404 Not Found`, если событие не найдено

Пример:

```http
GET /events/b1c7f2e5-1f7c-4b0c-a6f7-9e1a12345678
```

### 4.1. Получение топ-10 популярных событий

- **Сервис:** Events
- **Метод:** `GET`
- **URL:** `http://localhost:5002/events/top`
- **Тело запроса:** нет
- **Ответ:** `200 OK` — список `EventResponseDto`

События сортируются по проценту проданных мест: `(totalSeats - availableSeats) / totalSeats`.

Пример:

```http
GET /events/top
```

### 5. Создание события

- **Сервис:** Events
- **Метод:** `POST`
- **URL:** `http://localhost:5002/events`
- **Доступ:** роль `Admin`
- **Тело запроса:** `CreateEventDto`
- **Ответ:** `201 Created` — `EventResponseDto`
- **Ответ:** `400 Bad Request`, если данные невалидны

Пример:

```http
POST /events
Authorization: Bearer <ADMIN_TOKEN>
Content-Type: application/json

{
  "title": "Conference",
  "description": "Demo event",
  "startAt": "2026-08-01T10:00:00Z",
  "endAt": "2026-08-01T12:00:00Z",
  "totalSeats": 10
}
```

### 6. Обновление события

- **Сервис:** Events
- **Метод:** `PUT`
- **URL:** `http://localhost:5002/events/{id}`
- **Доступ:** роль `Admin`
- **Тело запроса:** `UpdateEventDto`
- **Ответ:** `204 No Content`
- **Ответ:** `404 Not Found`, если событие не найдено

Пример:

```http
PUT /events/a0821e5e-2163-46cd-95a5-f05cc2febd84
Authorization: Bearer <ADMIN_TOKEN>
Content-Type: application/json

{
  "title": "Updated conference",
  "description": "Updated demo event",
  "startAt": "2026-08-01T10:00:00Z",
  "endAt": "2026-08-01T13:00:00Z"
}
```

### 7. Удаление события

- **Сервис:** Events
- **Метод:** `DELETE`
- **URL:** `http://localhost:5002/events/{id}`
- **Доступ:** роль `Admin`
- **Тело запроса:** нет
- **Ответ:** `204 No Content`
- **Ответ:** `404 Not Found`, если событие не найдено

Пример:

```http
DELETE /events/b1c7f2e5-1f7c-4b0c-a6f7-9e1a12345678
Authorization: Bearer <ADMIN_TOKEN>
```

### 8. Создание брони

- **Сервис:** Bookings
- **Метод:** `POST`
- **URL:** `http://localhost:5003/events/{eventId}/book`
- **Доступ:** авторизованный пользователь
- **Тело запроса:** нет
- **Заголовок ответа:** `Location` — ссылка на созданную бронь
- **Ответ:** `202 Accepted` — бронь создана со статусом `Pending`
- **Ответ:** `409 Conflict`, если пользователь превысил лимит активных броней

Пример:

```http
POST /events/26aa576d-ed02-4bd5-847b-4a23786ca67d/book
Authorization: Bearer <USER_TOKEN>
```

Пример ответа:

```json
{
  "id": "b1c7f2e5-1f7c-4b0c-a6f7-9e1a12345678",
  "eventId": "26aa576d-ed02-4bd5-847b-4a23786ca67d",
  "userId": "a1c7f2e5-1f7c-4b0c-a6f7-9e1a12345678",
  "seats": 1,
  "status": 0,
  "createdAt": "2026-07-04T10:00:00Z",
  "processedAt": null
}
```

### 9. Получение информации о брони

- **Сервис:** Bookings
- **Метод:** `GET`
- **URL:** `http://localhost:5003/bookings/{id}`
- **Доступ:** авторизованный пользователь
- **Тело запроса:** нет
- **Ответ:** `200 OK` — `BookingResponseDto`
- **Ответ:** `404 Not Found`, если бронь не найдена

Пример:

```http
GET /bookings/b1c7f2e5-1f7c-4b0c-a6f7-9e1a12345678
Authorization: Bearer <USER_TOKEN>
```

### 10. Отмена брони

- **Сервис:** Bookings
- **Метод:** `DELETE`
- **URL:** `http://localhost:5003/bookings/{id}`
- **Доступ:** авторизованный пользователь
- **Тело запроса:** нет
- **Ответ:** `204 No Content`
- **Ответ:** `403 Forbidden`, если пользователь пытается отменить чужую бронь
- **Ответ:** `404 Not Found`, если бронь не найдена
- **Ответ:** `409 Conflict`, если бронь уже отменена

Пример:

```http
DELETE /bookings/b1c7f2e5-1f7c-4b0c-a6f7-9e1a12345678
Authorization: Bearer <USER_TOKEN>
```

## Фоновая обработка бронирований

В Bookings реализован фоновый сервис (`BackgroundService`), который обрабатывает бронирования.

Логика работы:

1. Сервис периодически проверяет список броней.
2. Находит брони со статусом `Pending`.
3. Для каждой такой брони выполняет задержку от 1 до 5 секунд.
4. Меняет статус на `Confirmed`.
5. Заполняет `ProcessedAt`.
6. Сохраняет изменения в базу Bookings.
7. Публикует `BookingConfirmed` в Kafka.

Клиент получает результат не сразу. Для финального статуса нужно повторно запросить бронь через `GET /bookings/{id}`.

## Проверка полного сценария

1. Зарегистрируйте администратора в Users.
2. Получите JWT администратора.
3. Создайте событие в Events и запомните `availableSeats`.
4. Зарегистрируйте обычного пользователя.
5. Получите JWT пользователя.
6. Создайте бронь в Bookings.
7. Дождитесь подтверждения брони фоновым сервисом.
8. Запросите событие в Events.
9. Убедитесь, что `availableSeats` уменьшилось.
