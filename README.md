# Food Delivery Application - DDD Implementation

A Domain-Driven Design (DDD) food delivery system with three isolated bounded contexts communicating via Azure Service Bus.

## Architecture Overview

```
┌──────────────┐  order-topic   ┌──────────────┐  billing-topic  ┌──────────────┐
│    Order     │───────────────>│   Billing    │────────────────>│   Delivery   │
│   Context    │ OrderPlaced    │   Context    │ InvoiceIssued   │   Context    │
└──────────────┘                └──────────────┘                 └──────────────┘
     OrderDb                          BillingDb                       DeliveryDb
```

## Project Structure

Each bounded context follows the same layered architecture:

```
{Context}/
├── {Context}.csproj                    # Host Worker Service
├── Program.cs                          # DI Configuration
├── appsettings.json                    # Configuration
├── Workers/                            # Event Listeners (BackgroundService)
│   └── {Event}Listener.cs
├── {Context}.Domain/                   # Core Business Logic
│   ├── Models/                         # Domain Models
│   │   ├── {Context}.cs               # State Records (IState interface)
│   │   ├── {ValueObject}.cs           # Value Objects with validation
│   │   ├── {Context}Dtos.cs           # Data Transfer Objects
│   │   └── {Context}Events.cs         # Domain Events
│   ├── Operations/                     # State Transformations
│   │   ├── DomainOperation.cs         # Base operation class
│   │   ├── {Context}Operation.cs      # Abstract operation with switch
│   │   └── {Specific}Operation.cs     # Concrete operations
│   ├── Workflows/                      # Business Workflows
│   │   └── {Workflow}Workflow.cs      # Orchestrates operations
│   ├── Repositories/                   # Repository Interfaces
│   │   ├── IEventSender.cs
│   │   └── I{Context}Repository.cs
│   ├── Exceptions/                     # Custom Exceptions
│   └── Extensions/                     # Extension Methods
├── {Context}.Data/                     # Data Access Layer
│   ├── {Context}DbContext.cs          # EF Core DbContext
│   ├── Models/                         # EF Entities
│   └── Repositories/                   # Repository Implementations
├── {Context}.Events/                   # Event Abstractions
│   └── IEventSender.cs                # Event sender interface
└── {Context}.Events.ServiceBus/       # Azure Service Bus Implementation
    ├── ServiceBusSettings.cs          # Configuration model
    └── ServiceBusTopicEventSender.cs  # Publisher implementation
```

## Bounded Contexts

### 1. Order Context
**Responsibility**: Accept and validate customer food orders

**Domain Model States**:
- `UnvalidatedOrder` → Raw input
- `ValidatedOrder` → After validation
- `OrderPlaced` → Success state (persisted)
- `InvalidOrder` → Error state

**Value Objects**:
- `RestaurantId`: Format REST-XXXX (4 digits, e.g., REST-0001)
- `CustomerPhone`: 10 digits (formatting characters stripped)
- `DeliveryAddress`: Min 10 characters
- `OrderAmount`: Must be > 0

**Workflow**: `PlaceOrderWorkflow`
1. Validate order data
2. Persist successful orders to OrderDb
3. Publish `OrderPlacedEvent` to `order-topic`

**Operations**:
- `ValidateOrderOperation`: Validates all fields using value objects
- `PlaceOrderOperation`: Transitions ValidatedOrder → OrderPlaced

### 2. Billing Context (Event-Driven)
**Responsibility**: Generate invoices with tax calculations

**Domain Model States**:
- `UnprocessedInvoice` → From OrderPlacedEvent
- `CalculatedInvoice` → With tax (10% rate)
- `InvoiceIssued` → Success state (persisted)
- `InvalidInvoice` → Error state

**Value Objects**:
- `OrderReference`: RestaurantId + CustomerPhone
- `InvoiceAmount`: Order amount (must be > 0)
- `TaxAmount`: Calculated tax (≥ 0)
- `TotalAmount`: Amount + Tax

**Workflow**: `IssueInvoiceWorkflow`
1. Receives `OrderPlacedEvent` from `order-topic/billing-subscription`
2. Calculate 10% tax
3. Persist invoice to BillingDb
4. Publish `InvoiceIssuedEvent` to `billing-topic`

**Operations**:
- `CalculateInvoiceOperation`: Validates and calculates tax
- `IssueInvoiceOperation`: Transitions CalculatedInvoice → InvoiceIssued

**Worker**: `OrderPlacedListener` (BackgroundService)

### 3. Delivery Context (Event-Driven)
**Responsibility**: Assign drivers and start deliveries

**Domain Model States**:
- `RequestedDelivery` → From InvoiceIssuedEvent
- `AssignedDelivery` → Driver + Route assigned
- `DeliveryStarted` → Success state (persisted)
- `FailedDelivery` → Error state

**Value Objects**:
- `InvoiceReference`: RestaurantId + CustomerPhone
- `DeliveryDestination`: Delivery address
- `DriverId`: Format DRV-XXXX (auto-generated)
- `DeliveryRoute`: Optimized route string

**Workflow**: `StartDeliveryWorkflow`
1. Receives `InvoiceIssuedEvent` from `billing-topic/delivery-subscription`
2. Assign random driver (DRV-XXXX)
3. Generate delivery route
4. Persist delivery to DeliveryDb
5. Publish `DeliveryStartedEvent` to `delivery-topic`

**Operations**:
- `AssignDeliveryOperation`: Validates and assigns driver/route
- `StartDeliveryOperation`: Transitions AssignedDelivery → DeliveryStarted

**Worker**: `InvoiceIssuedListener` (BackgroundService)

## Key Design Patterns

### 1. State Machine with Switch Expressions
Each operation uses pattern matching to handle different states:

```csharp
public override IOrder Transform(IOrder order) => order switch
{
    UnvalidatedOrder unvalidated => OnUnvalidated(unvalidated),
    ValidatedOrder validated => OnValidated(validated),
    OrderPlaced placed => OnPlaced(placed),
    InvalidOrder invalid => OnInvalid(invalid),
    _ => throw new InvalidOrderException($"Invalid state: {order.GetType().Name}")
};
```

### 2. Immutable State Records
States are immutable records that progress through the workflow:

```csharp
public record ValidatedOrder(
    RestaurantId Restaurant,
    CustomerPhone Phone,
    DeliveryAddress Address,
    OrderAmount Amount
) : IOrder;
```

### 3. Value Objects with Validation
Domain validation is enforced through value object constructors:

```csharp
public record RestaurantId
{
    public string Value { get; }
    
    private RestaurantId(string value)
    {
        if (IsValid(value)) Value = value.ToUpperInvariant();
        else throw new InvalidOrderException("Invalid restaurant ID...");
    }
    
    public static RestaurantId Create(string value) => new(value);
}
```

### 4. Persistence Strategy
**Only final successful states are persisted** to the database:

```csharp
if (order is OrderPlaced placed)
{
    await _orderRepository.SaveAsync(order);  // Only on success
    await PublishToServiceBusAsync(placed);
}
```

### 5. Event-Driven Communication
Contexts communicate exclusively via Azure Service Bus:

```csharp
// Publisher
await _eventSender.SendAsync("order-topic", orderMessage);

// Consumer (BackgroundService)
_processor.ProcessMessageAsync += async (args) =>
{
    var data = JsonSerializer.Deserialize<OrderPlacedMessageDto>(args.Message.Body);
    await _workflow.ExecuteAsync(data);
    await args.CompleteMessageAsync(args.Message);
};
```

## Configuration

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "{Context}Db": "Server=tcp:localhost,1433;Initial Catalog={Context}Db;..."
  },
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://namespace.servicebus.windows.net/;...",
    "TopicName": "input-topic",
    "SubscriptionName": "context-subscription",
    "OutputTopicName": "output-topic"
  }
}
```

### Dependency Injection Pattern

```csharp
// DbContext (Scoped)
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

// Repositories (Scoped)
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Event Sender (Singleton - expensive to create)
builder.Services.AddSingleton<IEventSender, ServiceBusTopicEventSender>();

// Workflow (Scoped with factory)
builder.Services.AddScoped<PlaceOrderWorkflow>(sp => 
{
    var eventSender = sp.GetRequiredService<IEventSender>();
    var orderRepo = sp.GetRequiredService<IOrderRepository>();
    var settings = sp.GetRequiredService<IOptions<ServiceBusSettings>>();
    
    return new PlaceOrderWorkflow(eventSender, orderRepo, settings.Value.TopicName);
});

// Worker (Hosted Service)
builder.Services.AddHostedService<OrderPlacedListener>();
```

## Azure Resources Required

### 1. Azure SQL Databases (3)
- OrderDb
- BillingDb
- DeliveryDb

### 2. Azure Service Bus Namespace
**Topics**:
- `order-topic`
- `billing-topic`
- `delivery-topic`

**Subscriptions**:
- `order-topic/billing-subscription` (consumed by Billing)
- `billing-topic/delivery-subscription` (consumed by Delivery)

## Building and Running

### Prerequisites
- .NET 9.0 SDK
- SQL Server (Local or Azure)
- Azure Service Bus namespace

### Build All Contexts
```bash
cd DeliveryApp

# Build Order
cd Order && dotnet build && cd ..

# Build Billing
cd Billing && dotnet build && cd ..

# Build Delivery
cd Delivery && dotnet build && cd ..
```

### Database Setup
```bash
# Order Context
cd Order
dotnet ef migrations add InitialCreate --context OrderDbContext --output-dir Migrations
dotnet ef database update --context OrderDbContext
# Run restaurant data script
sqlcmd -S localhost -d OrderDb -i Order.Data/Migrations/001_AddRestaurants.sql
# Run idempotency migration
sqlcmd -S localhost -d OrderDb -i Order.Data/Migrations/002_AddIdempotency.sql

# Billing Context
cd ../Billing
dotnet ef migrations add InitialCreate --context BillingDbContext --output-dir Migrations
dotnet ef database update --context BillingDbContext
sqlcmd -S localhost -d BillingDb -i Billing.Data/Migrations/002_AddIdempotency.sql

# Delivery Context
cd ../Delivery
dotnet ef migrations add InitialCreate --context DeliveryDbContext --output-dir Migrations
dotnet ef database update --context DeliveryDbContext
sqlcmd -S localhost -d DeliveryDb -i Delivery.Data/Migrations/002_AddIdempotency.sql
```

### Run Services (Separate Terminals)
```bash
# Terminal 1: Order Service
cd Order && dotnet run

# Terminal 2: Billing Service
cd Billing && dotnet run

# Terminal 3: Delivery Service
cd Delivery && dotnet run
```

## Error Handling Strategy

### Idempotency (✅ Implemented)
All event listeners now implement message deduplication:
- **ProcessedMessages table** tracks message IDs across all contexts
- Messages are checked before processing to prevent duplicates
- Same database transaction for business data and tracking
- Automatic skip on re-delivery with proper logging

### Advanced Error Handling
Event listeners implement sophisticated error categorization:

```csharp
try
{
    // Check idempotency
    if (await IsAlreadyProcessed(messageId)) {
        await args.CompleteMessageAsync(args.Message);
        return;
    }
    
    var result = await workflow.ExecuteAsync(data);
    
    // Record as processed
    await MarkAsProcessed(messageId);
    await args.CompleteMessageAsync(args.Message);
}
catch (JsonException ex)
{
    // Permanent error - immediate dead-letter
    await args.DeadLetterMessageAsync(args.Message, "JsonError", ex.Message);
}
catch (InvalidOperationException ex)
{
    // Business logic error - dead-letter
    await args.DeadLetterMessageAsync(args.Message, "BusinessLogicError", ex.Message);
}
catch (DbUpdateException ex)
{
    // Temporary DB error - retry
    await args.AbandonMessageAsync(args.Message);
}
catch (Exception ex)
{
    // Max 3 retries before dead-letter
    if (args.Message.DeliveryCount >= 3)
        await args.DeadLetterMessageAsync(args.Message, "MaxRetriesExceeded", ex.Message);
    else
        await args.AbandonMessageAsync(args.Message);
}
```

### Repository Validation
All repositories enforce workflow completion:
- **OrderRepository**: Only persists `OrderPlaced` or `InvalidOrder`
- **InvoiceRepository**: Only persists `InvoiceIssued` or `InvalidInvoice`  
- **DeliveryRepository**: Only persists `DeliveryStarted` or `FailedDelivery`

Prevents saving incomplete state transitions with clear exception messages.

## Testing the Flow

### 1. Place an Order (Interactive UI)
The Order service includes an interactive console UI:
```bash
cd Order && dotnet run
# Use arrow keys to select restaurant
# Enter customer phone, address, and amount
# View real-time success/error feedback
```

Or programmatically:
```csharp
var order = new PlaceOrderDto
{
    RestaurantId = "REST-0001",  // Note: Correct format with dash
    CustomerPhone = "5551234567",
    DeliveryAddress = "123 Main St, City",
    OrderAmount = 25.99m
};
```

### 2. Observe Event Flow
- **Order** saves to OrderDb → publishes to `order-topic`
- **Billing** consumes from `order-topic` → calculates tax → saves to BillingDb → publishes to `billing-topic`
- **Delivery** consumes from `billing-topic` → assigns driver → saves to DeliveryDb → publishes to `delivery-topic`

### 3. Test Idempotency
```sql
-- Manually re-deliver a message by querying ProcessedMessages
SELECT * FROM ProcessedMessages WHERE ProcessorName = 'OrderPlacedListener';

-- Delete the record to simulate re-delivery
DELETE FROM ProcessedMessages WHERE MessageId = 'your-message-id';

-- Observe: Second processing is skipped with log message
```

### 4. Check Logs
Each service logs:
- Message received with ID
- Idempotency check result
- Workflow execution
- Message published
- Detailed error categorization
- Dead-letter events

## Project Statistics

- **Total Files**: 101 (C#, csproj, json, sql)
- **C# Code Files**: 86
- **Lines of Code**: ~4,200
- **Bounded Contexts**: 3
- **Projects per Context**: 5
- **Database Tables**: 7 (Orders, Invoices, Deliveries, Restaurants, 3x ProcessedMessages)
- **Service Bus Topics**: 3
- **Background Workers**: 3 (OrderRequestListener, OrderPlacedListener, InvoiceIssuedListener)

## Recent Enhancements

### ✅ Completed
1. **Idempotency**: Message ID tracking with ProcessedMessages table
2. **Dead-Letter Queues**: Sophisticated error categorization and DLQ routing
3. **Repository Validation**: State machine enforcement at persistence layer
4. **Delivery Address Flow**: Proper event payload propagation
5. **Interactive UI**: Arrow-key restaurant selection with visual feedback
6. **Thread Safety**: SemaphoreSlim for console synchronization
7. **Error Handling**: Retry limits, structured logging, and message tracking

### 🔮 Future Enhancements
1. **Compensation**: Implement Saga pattern for rollbacks
2. **API Layer**: Add REST APIs for Order placement
3. **Event Sourcing**: Store all state transitions
4. **CQRS**: Separate read/write models
5. **Driver Pool**: Real driver availability management
6. **Route Optimization**: Integrate with mapping services
7. **Monitoring**: Add Application Insights telemetry
8. **Cleanup Jobs**: Automated ProcessedMessages table maintenance
9. **Correlation IDs**: Distributed tracing across contexts
10. **Health Checks**: Service Bus connectivity monitoring

## License

This is an educational project demonstrating Domain-Driven Design and event-driven microservices architecture.
