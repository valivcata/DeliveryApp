# Food Delivery Application - DDD Implementation

A Domain-Driven Design (DDD) food delivery system with three isolated bounded contexts communicating via Azure Service Bus, mirroring the architecture of Proiect_PSSC.

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
- `RestaurantId`: Format REST-XXXX (4 digits)
- `CustomerPhone`: 10 digits
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

# Billing Context
cd ../Billing
dotnet ef migrations add InitialCreate --context BillingDbContext --output-dir Migrations
dotnet ef database update --context BillingDbContext

# Delivery Context
cd ../Delivery
dotnet ef migrations add InitialCreate --context DeliveryDbContext --output-dir Migrations
dotnet ef database update --context DeliveryDbContext
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

Following the Proiect_PSSC pattern:

1. **No Idempotency**: Simple MVP without message deduplication
2. **Basic Retry**: Service Bus uses default retry with abandon/complete
3. **Minimal Dead-Letter**: No DLQ usage (can be added later)
4. **Exception Handling**: Try-catch in workers with abandon on error

```csharp
try
{
    var result = await workflow.ExecuteAsync(data);
    await args.CompleteMessageAsync(args.Message);  // Success
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing message");
    await args.AbandonMessageAsync(args.Message);  // Retry
}
```

## Testing the Flow

### 1. Place an Order (Order Context)
Execute the workflow manually or via HTTP endpoint:
```csharp
var order = new PlaceOrderDto
{
    RestaurantId = "REST-1234",
    CustomerPhone = "5551234567",
    DeliveryAddress = "123 Main St, City",
    OrderAmount = 25.99m
};
```

### 2. Observe Event Flow
- **Order** saves to OrderDb → publishes to `order-topic`
- **Billing** consumes from `order-topic` → calculates tax → saves to BillingDb → publishes to `billing-topic`
- **Delivery** consumes from `billing-topic` → assigns driver → saves to DeliveryDb → publishes to `delivery-topic`

### 3. Check Logs
Each service logs:
- Message received
- Workflow execution
- Message published
- Errors (if any)

## Project Statistics

- **Total Files**: 92 (C#, csproj, json)
- **C# Code Files**: 77
- **Lines of Code**: ~3,500
- **Bounded Contexts**: 3
- **Projects per Context**: 5
- **Database Tables**: 3 (Orders, Invoices, Deliveries)
- **Service Bus Topics**: 3
- **Background Workers**: 2 (Billing, Delivery)

## Key Differences from Proiect_PSSC

1. **Domain**: Food delivery vs Travel booking
2. **Contexts**: Order/Billing/Delivery vs Booking/Payment/Ticketing
3. **Tax Calculation**: 10% tax in Billing context
4. **Driver Assignment**: Random driver allocation in Delivery
5. **Event Names**: OrderPlaced/InvoiceIssued/DeliveryStarted

## Future Enhancements

1. **Idempotency**: Add message ID tracking to prevent duplicate processing
2. **Dead-Letter Queues**: Handle poison messages
3. **Compensation**: Implement Saga pattern for rollbacks
4. **API Layer**: Add REST APIs for Order placement
5. **Event Sourcing**: Store all state transitions
6. **CQRS**: Separate read/write models
7. **Delivery Address**: Include address in invoice events
8. **Driver Pool**: Real driver availability management
9. **Route Optimization**: Integrate with mapping services
10. **Monitoring**: Add Application Insights telemetry

## License

This is an educational project mirroring the Proiect_PSSC architecture pattern.
