---
description: OrderFlow Order Management System - Copilot Coding Standards
referenceDocs:
  - ../README.md
  - ../architecture/
applyTo:
  - "**/*.cs"
  - "**/*.tsx"
  - "**/*.ts"
---

# OrderFlow - Copilot Coding Standards

**ALWAYS reference [README.md](../README.md) as the single source of truth for:**

- Architecture diagrams (all 7 diagrams)
- Design patterns and their implementation
- Database design and entity definitions
- Testing strategy
- Technology stack
- Security and observability

This file contains coding standards and implementation guidelines.

---

## Quick Architecture Reference

See [README.md](../README.md) for:

- [High-Level System Architecture](../README.md#high-level-system-architecture)
- [Solution Structure](../README.md#solution-structure)
- [Why a Contracts Project Exists](../README.md#why-a-contracts-project-exists)
- [Controller-Service-Repository Pattern](../README.md#controller-service-repository-pattern)
- [Order Creation Flow](../README.md#order-creation-flow)
- [Outbox Pattern](../README.md#outbox--rabbitmq-flow)
- [Order Lifecycle & State Transitions](../README.md#order-lifecycle)
- [Idempotent Status Updates](../README.md#idempotent-status-updates)
- [OpenAPI First Development](../README.md#openapi-first-development)
- [Generated React Client](../README.md#generated-react-client)

---

## Project Organization

Full details in [README.md - Solution Structure](../README.md#solution-structure):

**Key Projects:**

- `OrderManagement.Api` - Controllers, middleware, filters, swagger configuration
- `OrderManagement.Application` - Services, validators, messaging, mapping
- `OrderManagement.Domain` - Entities, enums, constants, business rules
- `OrderManagement.Infrastructure` - Repositories, EF Core, RabbitMQ, Outbox, migrations
- `OrderManagement.Contracts` - Shared DTOs, requests, responses, events, interfaces
- `OrderManagement.Worker` - Message consumers, publishers, background services
- Test projects - Unit, API, integration tests

---

## CSR Pattern Responsibilities

Full details in [README.md - Controller-Service-Repository Pattern](../README.md#controller-service-repository-pattern)

**Controller:**

- Request handling
- Response handling
- Swagger documentation
- Model validation
- Logging
- ❌ NO business logic
- ❌ NO database access

**Service:**

- Business rules
- Validation
- State transitions
- Event creation
- Domain orchestration
- ❌ NO HTTP concerns
- ❌ NO SQL queries

**Repository:**

- Database access
- Query execution
- Persistence
- ❌ NO business rules

---

## API Standards

Every endpoint must:

- Be fully documented with Swagger and `[SwaggerOperation]` attribute
- Return strongly typed DTOs from `OrderManagement.Contracts`
- Return consistent error responses with proper HTTP status codes
- Use async/await
- Include structured logging with CorrelationId
- Include exception handling at controller level only
- Support idempotent operations where appropriate (status updates)

**Example Controller Pattern:**

```csharp
[HttpPost("orders")]
[SwaggerOperation(Summary = "Create a new order", Description = "...")]
[ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    try
    {
        var result = await _service.CreateOrderAsync(request);
        _logger.LogInformation("Order created: {OrderId}", result.Id);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning("Validation failed: {Message}", ex.Message);
        return BadRequest(new ApiErrorResponse(ex.Message));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "OrdersController->CreateOrder failed");
        return StatusCode(StatusCodes.Status500InternalServerError,
            new ApiErrorResponse("An unexpected error occurred"));
    }
}
```

---

## Swagger/OpenAPI Documentation

**Required for every endpoint:**

```csharp
[ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
[SwaggerOperation(
    Summary = "Brief endpoint summary",
    Description = "Detailed description of what this endpoint does"
)]
```

**Every DTO must have:**

```csharp
public class CreateOrderRequest
{
    [Required]
    [ApiProperty(Description = "...")]
    public string CustomerId { get; set; }

    [Required]
    [ApiProperty(Description = "...")]
    public decimal Amount { get; set; }
}
```

Swagger is enabled in all environments. Endpoint: `/swagger/v1/swagger.json`

---

## Logging Standards

Use Serilog with structured logging.

**Every controller exception must log:**

```csharp
_logger.LogError(ex, "OrdersController->CreateOrder");
```

**Include context properties:**

```csharp
_logger.LogInformation("Order created", new { OrderId = result.Id, CustomerId = result.CustomerId });
```

**All background workers must log:**

- Message received
- Message processing started
- Processing outcome (success/failure)
- Retry attempts
- Exceptions with full context

**Structured properties to include:**

- `CorrelationId`
- `RequestId`
- `OrderId`
- `CustomerId`
- `EventType`

Never swallow exceptions - always log and propagate or handle appropriately.

---

## Entity Framework & Migrations

Use EF Core Code First.

**Core Entities** (see [README.md - Database Design](../README.md#database-design)):

- `Customer` - Id, Name, Email, CountryCode, CreatedAt
- `Order` - Id, CustomerId, Status, CurrencyCode, TotalAmount, CreatedAt, RowVersion
- `OrderLineItem` - Id, OrderId, ProductSku, Quantity, UnitPrice
- `OutboxMessage` - Id, EventType, Payload, Processed, CreatedAt

**Optimistic Concurrency:**

Orders use `RowVersion` for concurrency control:

```csharp
[Timestamp]
public byte[] RowVersion { get; set; }
```

**Migration Commands:**

```bash
dotnet ef migrations add {MigrationName}
dotnet ef database update
dotnet ef migrations script -o migrations.sql
```

---

## DTOs & Contracts

**ALL DTOs must:**

- Be defined in `OrderManagement.Contracts`
- Include full `[ApiProperty]` decorators
- Be re-exported from domain projects as needed
- Never be redefined in controllers

**Frontend must:**

- Use ONLY generated OpenAPI client types
- Never manually create API DTOs
- Never manually write request/response types
- Regenerate client after any API changes

**Command to regenerate frontend client:**

```bash
npm run generate:client
```

---

## State Transitions & Business Rules

See [README.md - Order Lifecycle](../README.md#order-lifecycle) for allowed state transitions.

**Service layer must validate:**

- Current status can transition to requested status
- All business rules are satisfied
- Idempotency-Key is checked (if provided)
- Status-specific validations

Example:

```csharp
private void ValidateStatusTransition(Order order, OrderStatus newStatus)
{
    var allowedTransitions = new Dictionary<OrderStatus, OrderStatus[]>
    {
        { OrderStatus.Pending, new[] { OrderStatus.Paid, OrderStatus.Cancelled } },
        { OrderStatus.Paid, new[] { OrderStatus.Fulfilled, OrderStatus.Cancelled } }
    };

    if (!allowedTransitions.ContainsKey(order.Status) ||
        !allowedTransitions[order.Status].Contains(newStatus))
    {
        throw new InvalidOperationException($"Cannot transition from {order.Status} to {newStatus}");
    }
}
```

---

## Idempotency

Endpoints that modify status must support:

```http
Idempotency-Key: <guid>
```

See [README.md - Idempotent Status Updates](../README.md#idempotent-status-updates) for diagram and pattern.

**Implementation:**

1. Extract `Idempotency-Key` header from request
2. Check idempotency store for previous response
3. If found, return cached response
4. If not found, execute operation and cache response
5. Always return same result for same key

---

## RabbitMQ & Outbox Pattern

See [README.md - Reliable Messaging](../README.md#outbox--rabbitmq-flow).

**Key points:**

- Events created in same transaction as data changes
- Publisher Worker polls Outbox table every 5 seconds
- Events published to RabbitMQ
- Worker processes events asynchronously
- Marked processed only after successful publish

This ensures no message loss and eventual consistency.

---

## Validation

**Request validation:**

```csharp
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.CountryCode)
            .Must(IsValidCountryCode)
            .WithMessage("Invalid country code");
    }
}
```

---

## Regional Validation

See [README.md - SADC Regional Validation](../README.md#sadc-regional-validation).

Validate:

- ISO 3166-1 Alpha-2 country codes
- ISO 4217 currency codes
- Common Monetary Area (CMA) relationships

Example: ZA→ZAR, BW→BWP, NA→NAD, LS→LSL, SZ→SZL

---

## Testing

See [README.md - Testing Strategy](../README.md#testing-strategy).

**Test Organization:**

- **Unit Tests** - Business logic, validations, state transitions
- **API Tests** - Endpoint contract testing, HTTP semantics
- **Integration Tests** - EF Core, RabbitMQ, Outbox, end-to-end workflows

**Coverage targets:**

- Order calculations
- Status transitions
- Currency validation
- Idempotency validation
- Create/read/update operations
- Error handling

---

## Docker Environment

See [README.md - Docker Environment](../README.md#docker-environment).

Services:

- SQL Server
- RabbitMQ
- API
- Worker
- React Web

**Start everything:**

```bash
docker-compose up -d
```

---

## GraphQL Endpoint

See [README.md - API Endpoints - GraphQL](../README.md#graphql-endpoint) for query examples and benefits.

**Location:** `/OrderManagement.Api/GraphQL/`

**Files:**

- `Query.cs` - Resolvers for root queries
- `Types.cs` - GraphQL type definitions (OrderType, CustomerType, OrderLineItemType)
- `schema.graphql` - Schema documentation with examples

**Query Capabilities:**

- Get orders with filtering by customerId and status
- Get order details with line items and associated customer
- Get customers with related orders
- Single queries for nested data (no N+1 without DataLoaders)

**When to use GraphQL vs REST:**

| REST                                  | GraphQL                        |
| ------------------------------------- | ------------------------------ |
| Simple CRUD operations                | Complex multi-resource queries |
| Mobile clients with limited bandwidth | Dashboard/reporting queries    |
| Strongly typed external API           | Internal flexible API          |

**GraphQL provides:**

- Single request for nested data (orders + customers + line items)
- Client control over which fields to fetch
- Reduced network traffic for constrained clients
- N+1 query prevention strategy

---

## Technology Stack

See [README.md - Technology Stack](../README.md#technology-stack).

**Backend:** ASP.NET Core 8, EF Core, SQL Server, RabbitMQ, Swagger, Serilog, OpenTelemetry, xUnit

**Frontend:** React, TypeScript, React Query, Generated OpenAPI Client

**Infrastructure:** Docker Compose

---

## Getting Started

See [README.md - Docker Environment](../README.md#docker-environment) and development setup instructions.

---

**Always reference [README.md](../README.md) for architecture decisions, patterns, diagrams, and design rationale.**
