# OTel Reference Project

A .NET 10 Clean Architecture reference project demonstrating **OpenTelemetry** integration with multiple observability backends and **Apache Kafka** event-driven messaging.

## Architecture

```
OTel.Api              → ASP.NET Core Web API (entry point, DI, middleware, background services)
OTel.Application      → Business logic, services, DTOs, validators, interfaces
OTel.Domain           → Entities, domain events, enums, common abstractions
OTel.Infrastructure   → EF Core DbContext, Kafka producer, migrations
```

## Tech Stack

| Category | Technology |
|----------|-----------|
| Framework | .NET 10, ASP.NET Core |
| Database | PostgreSQL (Npgsql) |
| ORM | Entity Framework Core 10 |
| Logging | Serilog (Console + OpenTelemetry sinks) |
| Validation | FluentValidation |
| Mapping | Mapster |
| Messaging | Apache Kafka (Confluent.Kafka) |
| Observability | OpenTelemetry (traces, metrics, logs) |
| API Docs | OpenAPI + Scalar |

## Observability Backends

| Service | URL | Purpose |
|---------|-----|---------|
| Jaeger | http://localhost:16686 | Distributed tracing UI |
| Prometheus | http://localhost:9090 | Metrics collection & querying |
| Grafana | http://localhost:3000 | Dashboards (user: `admin`, pass: `admin`) |
| Loki | http://localhost:3100 | Log aggregation (queryable via Grafana) |
| Kafka UI | http://localhost:8080 | Browse Kafka topics, messages, consumer groups |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Getting Started

### 1. Start Infrastructure

```bash
docker compose up -d
```

This starts PostgreSQL, Jaeger, Prometheus, Loki, Grafana, Kafka, and Kafka UI.

### 2. Run the API

```bash
dotnet run --project OTel.Api
```

The API starts on:
- **HTTPS:** https://localhost:7135
- **HTTP:** http://localhost:5192 (redirects to HTTPS)

### 3. Apply Database Migrations

EF Core migrations run automatically on startup. To manually apply:

```bash
dotnet ef database update --project OTel.Infrastructure --startup-project OTel.Api
```

### 4. Explore the API

Open **Scalar API docs** at https://localhost:7135/scalar/v1

## API Endpoints

### Products

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/product` | Get all products |
| GET | `/api/product/{id}` | Get product by ID |
| POST | `/api/product` | Create a product |
| PUT | `/api/product` | Update a product |
| DELETE | `/api/product/{id}` | Delete a product |

### Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/order` | Get all orders |
| GET | `/api/order/{id}` | Get order by ID |
| POST | `/api/order` | Create an order |

## Sample Requests

### Create a Product

```json
POST /api/product
{
  "name": "Mechanical Keyboard",
  "price": 149.99,
  "stock": 50
}
```

### Create an Order

```json
POST /api/order
{
  "productId": 1,
  "quantity": 2
}
```

## Kafka Event Flow

When an order is created:

1. `OrderService` saves the order with status **"Pending"**
2. An `OrderCreatedEvent` is published to the `order-events` Kafka topic
3. `OrderEventConsumerService` (background service) picks up the message
4. The consumer updates the order status from **"Pending"** to **"Confirmed"**

You can observe this flow in:
- **Kafka UI** (http://localhost:8080) — see messages in the `order-events` topic
- **Jaeger** (http://localhost:16686) — `Kafka.Produce` and `Kafka.Consume` spans
- **Prometheus** (http://localhost:9090) — `orders_events_published_total`, `orders_events_consumed_total` counters
- **App logs** — consumer logs each received event

## OpenTelemetry Instrumentation

### Traces (Jaeger)

Custom activity sources:
- `OTel.Api` — business operation spans (e.g., `OrderService.Create`, `ProductService.GetAll`)
- `OTel.Kafka` — messaging spans (`Kafka.Produce`, `Kafka.Consume`)

Auto-instrumented:
- ASP.NET Core (HTTP requests)
- HttpClient (outbound calls)
- Entity Framework Core (database queries)

### Metrics (Prometheus)

Custom meters:
- `products.queried` — number of product list queries
- `products.created` — number of products created
- `orders.created` — number of orders created
- `orders.events.published` — Kafka events published
- `orders.events.consumed` — Kafka events consumed

Prometheus scrapes the `/metrics` endpoint on https://localhost:7135.

### Logs (Loki)

Serilog sends structured logs to:
- **Console** — for local development
- **Loki** — via OTLP HTTP/Protobuf, queryable in Grafana
- **Jaeger** — via OTLP gRPC, correlated with traces

## Project Structure

```
OTel/
├── docker-compose.yml
├── prometheus.yml
├── loki-config.yml
├── grafana/provisioning/datasources/
│   └── datasources.yml
├── OTel.Api/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── ProductController.cs
│   │   └── OrderController.cs
│   ├── BackgroundServices/
│   │   └── OrderEventConsumerService.cs
│   └── Common/
│       ├── OpenTelemetryRegistration.cs
│       ├── ServiceRegistration.cs
│       ├── MapperRegistration.cs
│       └── MiddlewareService.cs
├── OTel.Application/
│   ├── DTO/
│   │   ├── Product/
│   │   └── Order/
│   ├── Interfaces/
│   │   ├── IProductService.cs
│   │   ├── IOrderService.cs
│   │   └── IEventProducer.cs
│   ├── Services/
│   │   ├── ProductService.cs
│   │   └── OrderService.cs
│   └── Validators/
├── OTel.Domain/
│   ├── Common/
│   │   ├── BaseEntity.cs
│   │   └── ResponseModel.cs
│   ├── Events/
│   │   └── OrderCreatedEvent.cs
│   ├── Models/
│   │   ├── Product.cs
│   │   └── Order.cs
│   └── Interfaces/
└── OTel.Infrastructure/
    ├── Context/
    │   ├── AppDbContext.cs
    │   └── AppDbContextExt.cs
    ├── Messaging/
    │   └── KafkaEventProducer.cs
    └── Migrations/
```

## Docker Services

| Service | Image | Port(s) |
|---------|-------|---------|
| PostgreSQL | `postgres:latest` | 5432 |
| Jaeger | `jaegertracing/jaeger:latest` | 4317, 16686 |
| Prometheus | `prom/prometheus:latest` | 9090 |
| Loki | `grafana/loki:latest` | 3100 |
| Grafana | `grafana/grafana:latest` | 3000 |
| Kafka | `apache/kafka:latest` | 9092 |
| Kafka UI | `provectuslabs/kafka-ui:latest` | 8080 |

## Database Access

```bash
docker exec -it otel-postgres-1 psql -U postgres -d OTelDb
```

```sql
SELECT * FROM "Products";
SELECT * FROM "Orders";
```

## Configuration

All configuration is in `OTel.Api/appsettings.json`:

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Serilog` | Log levels and overrides |
| `OpenTelemetry` | Service name, exporter toggles and endpoints |
| `Kafka` | Bootstrap servers, topic name, consumer group |
