## Сервис заказов с согласованием данных между микросервисами

### Общая архитектура:

Система состоит из компонентов:

- Order Service — управление заказами и оркестрация Saga

- Inventory Service — резервирование товаров

- Payment Service — эмуляция списания средств

- Notification Service — отправка уведомлений

- RabbitMQ — брокер сообщений

- PostgreSQL — отдельная база данных для каждого сервиса

- Shared библиотеки — классы событий, реализация OutBox

- Docker Compose — развертывание

Публикация событий в RabbitMQ происходит в OutBoxPublisher.

Получение событий из RabbitMQ происходит в Consumer-классах (Infrastructure/Consumer/<...>.cs)

### Процесс оформления заказа

#### Order Service:

1. Клиент отправляет POST /orders в Order Service;

2. Создаётся заказ со статусом Pending;

3. Order Service сохраняет событие OrderCreated в Outbox;

4. OutboxPublisher публикует событие в RabbitMQ.

#### Inventory Service:

1. OrderCreatedConsumer подключается к брокеру и получает события OrderCreated;

2. Резервирует товар из заказа;

3. Публикует StockReserved или StockFailed.

#### Payment Service:

1. StockReservedConsumer подключаетcя к брокеру и получает события StockReserved;

2. Выполняет списание средств;

3. публикует PaymentSucceeded или PaymentFailed.

#### Order Service:

1. Consumer классы подключаются к брокеру и получают события PaymentSucceeded, PaymentFailed, StockReserved;

2. Реагирует на события;

3. Обновляет статус заказа;

4. Выполняет компенсации при сбое.

#### Notification Service:

1. Подключается к брокеру и получает финальные события создания заказа (StockFailed, PaymentFailed, PaymentSuccess);

2. Отправляет уведомление пользователю;

### Статусы заказа

Поддерживаются следующие статусы:

- Pending — заказ создан

- Reserved — товар зарезервирован

- Paid — оплата прошла успешно

- Completed — заказ завершён

- Cancelled — заказ отменён (компенсация)

### Компоненты системы

#### Order Service

- HTTP API для создания заказов и получения статуса

- Хранение заказов в своей БД

- Saga

- Реакция на события Stock*, Payment*

- Публикация событий OrderCreated

#### Inventory Service

- Проверка и резервирование товаров

- Реакция на события OrderCreated

- Публикация событий:

    StockReserved

    StockFailed

- Своя бд (Таблицы: зарезервированные товары, доступные товары, OutBoxMessages)

#### Payment Service

- Эмуляция списания средств

- Публикация событий:

    PaymentSucceeded

    PaymentFailed

- Своя бд (Таблицы: Payment, OutBoxMessages)

#### Notification Service

- Получение событий:

    PaymentSucceeded

    PaymentFailed

    StockFailed

- Логирование и эмуляция отправки уведомлений

- Нет бд

#### RabbitMQ

- routingKey = имя события (PaymentSucceeded, StockFailed и т.д.)

- Каждое событие имеет собственную очередь

#### Consumers классы в сервисах:

- Работают в BackgroundService

- Подключаются к RabbitMQ и получают опубликованные события нужного типа, далее событие обрабатывается бизнес-логикой.

#### Shared.OutBox

##### Outbox Pattern

- Реализован во всех сервисах с БД в виде таблицы OutboxMessages.

OutboxMessage содержит:

- Id

- Type — тип события

- Payload — JSON события

- OccurredAt

- ProcessedAt

##### OutboxPublisher

- Generic-класс: OutboxPublisher<TDbContext>;

- Работает как BackgroundService;

- Периодически:

    Читает необработанные сообщения из OutBoxMessages всех сервисов с DBContext;

    публикует их в RabbitMQ;

    Помечает как обработанные;

#### Shared.Contracts

DTO и события:

- OrderCreated;

- StockReserved, StockFailed;

- PaymentSucceeded, PaymentFailed;

Используются всеми сервисами

#### PostgreSQL

Отдельная PostgreSQL база для каждого сервиса

#### Миграции:

- Создаются отдельно для каждого сервиса;

- Включают бизнес-таблицы и Outbox;

- При старте сервисы автоматически применяют миграции.

#### Docker и запуск

##### Docker Compose

Содержит:

- RabbitMQ;

- PostgreSQL (3 экземпляра);

- Все микросервисы;

##### Dockerfile

Создан для каждого сервиса