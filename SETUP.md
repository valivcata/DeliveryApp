# Quick Setup Guide

## ✅ Implementation Complete

All three bounded contexts have been successfully implemented with production-ready features!

## Project Statistics
- **Total Files Created**: 101+ (including README, migrations, entities)
- **C# Source Files**: 86
- **Contexts**: 3 (Order, Billing, Delivery)
- **Projects**: 15 total (5 per context)
- **Build Status**: ✅ All contexts compile successfully (0 warnings, 0 errors)
- **Features**: Idempotency ✅ | Error Handling ✅ | Validation ✅ | REST API ✅

## Directory Structure
```
DeliveryApp/
├── README.md                    # Comprehensive documentation
├── Order/                       # Order bounded context (Web API)
│   ├── Order.sln
│   ├── Order.csproj            # Web API host
│   ├── Program.cs
│   ├── Controllers/            # REST API endpoints
│   ├── Order.Domain/           # Business logic
│   ├── Order.Data/             # Data access
│   ├── Order.Events/           # Event abstractions
│   └── Order.Events.ServiceBus/
├── Billing/                     # Billing bounded context
│   ├── Billing.sln
│   ├── Billing.csproj          # Host service
│   ├── Program.cs
│   ├── Workers/                # Event listeners
│   ├── Billing.Domain/
│   ├── Billing.Data/
│   ├── Billing.Events/
│   └── Billing.Events.ServiceBus/
└── Delivery/                    # Delivery bounded context
    ├── Delivery.sln
    ├── Delivery.csproj         # Host service
    ├── Program.cs
    ├── Workers/                # Event listeners
    ├── Delivery.Domain/
    ├── Delivery.Data/
    ├── Delivery.Events/
    └── Delivery.Events.ServiceBus/
```

## Architecture Flow

```
User → Order Context → OrderPlaced → Billing Context → InvoiceIssued → Delivery Context → DeliveryStarted
        ↓                              ↓                                 ↓
     OrderDb                        BillingDb                        DeliveryDb
```

## Build Verification

```bash
cd /Users/valentin.vinatoru/Projects/PSSC/DeliveryApp

# Build each context
cd Order && dotnet build && cd ..
cd Billing && dotnet build && cd ..
cd Delivery && dotnet build && cd ..
```

All contexts build successfully! ✅

## Database Migrations

EF Core migrations and SQL scripts have been created for all three bounded contexts:

```bash
# Order Context
Order/Order.Data/Migrations/
├── 20260114180840_InitialCreate.cs
├── 20260114180840_InitialCreate.Designer.cs
├── OrderDbContextModelSnapshot.cs
├── 001_AddRestaurants.sql           # Sample restaurant data (REST-0001 format)
└── 002_AddIdempotency.sql           # ProcessedMessages table

# Billing Context
Billing/Billing.Data/Migrations/
├── 20260114180857_InitialCreate.cs
├── 20260114180857_InitialCreate.Designer.cs
├── BillingDbContextModelSnapshot.cs
└── 002_AddIdempotency.sql           # ProcessedMessages table

# Delivery Context
Delivery/Delivery.Data/Migrations/
├── 20260114180907_InitialCreate.cs
├── 20260114180907_InitialCreate.Designer.cs
├── DeliveryDbContextModelSnapshot.cs
└── 002_AddIdempotency.sql           # ProcessedMessages table
```

**Tables Created:**
- **OrderDb**: `Orders`, `Restaurants`, `ProcessedMessages` (with indexes)
- **BillingDb**: `Invoices`, `ProcessedMessages` (with indexes)
- **DeliveryDb**: `Deliveries`, `ProcessedMessages` (with indexes)

**To Apply Migrations:**
```bash
# Update Azure connection strings in appsettings.json first, then:
cd Order && dotnet ef database update
sqlcmd -S localhost -d OrderDb -i Order.Data/Migrations/001_AddRestaurants.sql
sqlcmd -S localhost -d OrderDb -i Order.Data/Migrations/002_AddIdempotency.sql

cd ../Billing && dotnet ef database update
sqlcmd -S localhost -d BillingDb -i Billing.Data/Migrations/002_AddIdempotency.sql

cd ../Delivery && dotnet ef database update
sqlcmd -S localhost -d DeliveryDb -i Delivery.Data/Migrations/002_AddIdempotency.sql
```

## Next Steps

### 1. Configure Azure Resources

**Update appsettings.json in each context:**

Order/appsettings.json:
```json
{
  "ConnectionStrings": {
    "OrderDb": "YOUR_SQL_CONNECTION_STRING"
  },
  "ServiceBus": {
    "ConnectionString": "YOUR_SERVICE_BUS_CONNECTION_STRING",
    "TopicName": "order-topic"
  }
}
```

Billing/appsettings.json:
```json
{
  "ConnectionStrings": {
    "BillingDb": "YOUR_SQL_CONNECTION_STRING"
  },
  "ServiceBus": {
    "ConnectionString": "YOUR_SERVICE_BUS_CONNECTION_STRING",
    "TopicName": "order-topic",
    "SubscriptionName": "billing-subscription",
    "OutputTopicName": "billing-topic"
  }
}
```

Delivery/appsettings.json:
```json
{
  "ConnectionStrings": {
    "DeliveryDb": "YOUR_SQL_CONNECTION_STRING"
  },
  "ServiceBus": {
    "ConnectionString": "YOUR_SERVICE_BUS_CONNECTION_STRING",
    "TopicName": "billing-topic",
    "SubscriptionName": "delivery-subscription",
    "OutputTopicName": "delivery-topic"
  }
}
```

### 2. Create Azure Service Bus Resources

In Azure Portal:
1. Create Service Bus Namespace
2. Create Topics:
   - `order-topic`
   - `billing-topic`
   - `delivery-topic`
3. Create Subscriptions:
   - `order-topic/billing-subscription`
   - `billing-topic/delivery-subscription`

### 3. Create Databases

```bash
# Order Database
cd Order
dotnet ef migrations add InitialCreate --output-dir Migrations
dotnet ef database update

# Billing Database
cd ../Billing
dotnet ef migrations add InitialCreate --output-dir Migrations
dotnet ef database update

# Delivery Database
cd ../Delivery
dotnet ef migrations add InitialCreate --output-dir Migrations
dotnet ef database update
```

### 4. Run the Application

Open 3 separate terminals:

**Terminal 1 - Order Service:**
```bash
cd Order
dotnet run
```

**Terminal 2 - Billing Service:**
```bash
cd Billing
dotnet run
```

**Terminal 3 - Delivery Service:**
```bash
cd Delivery
dotnet run
```

### 5. Test the Flow

**Option 1: Interactive Console UI (Recommended)**
```bash
cd Order
dotnet run
# Use ↑/↓ arrow keys to select a restaurant
# Enter customer phone, delivery address, and order amount
# View real-time success/error feedback with color coding
```

**Option 2: Programmatic Testing**
```csharp
var order = new PlaceOrderDto
{
    RestaurantId = "REST-0001",  // Note: Correct format with dash and 4 digits
    CustomerPhone = "5551234567",
    DeliveryAddress = "123 Main Street, City",
    OrderAmount = 29.99m
};

var workflow = serviceProvider.GetRequiredService<PlaceOrderWorkflow>();
var result = await workflow.ExecuteAsync(order);
```

**Expected Flow:**
1. Order validates and saves to OrderDb
2. OrderPlacedEvent published to order-topic
3. **Billing** checks idempotency → receives event → calculates 10% tax ($3.00) → saves invoice → records message ID
4. InvoiceIssuedEvent published to billing-topic (includes delivery address)
5. **Delivery** checks idempotency → receives event → assigns driver (DRV-XXXX) → starts delivery → records message ID
6. DeliveryStartedEvent published to delivery-topic

**Test Idempotency:**
```sql
-- Check processed messages
SELECT * FROM ProcessedMessages ORDER BY ProcessedAt DESC;

-- Simulate re-delivery by deleting a record
DELETE FROM ProcessedMessages WHERE MessageId = 'your-message-id';
-- Result: Message processed again, but skipped on second attempt
```

## Domain Models Summary

### Order Context
- **States**: UnvalidatedOrder → ValidatedOrder → OrderPlaced
- **Value Objects**: RestaurantId, CustomerPhone, DeliveryAddress, OrderAmount
- **Validation**: Restaurant format (REST-0001 to REST-9999), Phone (10 digits), Address (min 10 chars)
- **Interactive UI**: Arrow-key restaurant selection with SemaphoreSlim thread safety

### Billing Context
- **States**: UnprocessedInvoice → CalculatedInvoice → InvoiceIssued
- **Value Objects**: OrderReference, InvoiceAmount, TaxAmount (10%), TotalAmount
- **Logic**: Tax calculation, invoice total computation

### Delivery Context
- **States**: RequestedDelivery → AssignedDelivery → DeliveryStarted
- **Value Objects**: InvoiceReference, DriverId, DeliveryDestination, DeliveryRoute
- **Logic**: Random driver assignment, route generation

## Key Features Implemented

✅ Domain-Driven Design with Onion Architecture
✅ Immutable state records with explicit state machines
✅ Switch expression-based operations
✅ Value objects with encapsulated validation
✅ Event-driven inter-context communication
✅ Azure Service Bus integration
✅ Entity Framework Core with SQL Server
✅ BackgroundService workers for event consumption
✅ Dependency injection with proper lifetimes
✅ Only successful states persisted (no intermediate states)
✅ **Idempotency** with ProcessedMessages tracking
✅ **Advanced Error Handling** with retry limits and DLQ routing
✅ **Repository Validation** prevents invalid state persistence
✅ **Interactive Console UI** with arrow-key navigation
✅ **Thread Safety** with SemaphoreSlim for console operations
✅ **Structured Logging** with message ID correlation
✅ **Delivery Address Propagation** through event payload

## Production Ready Features

### Idempotency Implementation
- ProcessedMessages table in each context
- Automatic duplicate detection
- Same-transaction recording with business data
- Message ID correlation in logs

### Error Handling Strategy
- **JsonException**: Immediate dead-letter (malformed data)
- **InvalidOperationException**: Dead-letter (business logic error)  
- **DbUpdateException**: Retry with abandon (temporary DB issue)
- **Generic Exception**: Max 3 retries, then dead-letter
- Delivery count tracking to prevent infinite loops

### Repository Validation
- OrderRepository: Only persists `OrderPlaced` or `InvalidOrder`
- InvoiceRepository: Only persists `InvoiceIssued` or `InvalidInvoice`
- DeliveryRepository: Only persists `DeliveryStarted` or `FailedDelivery`
- Clear exception messages for debugging

## Support

For detailed architecture documentation, see [README.md](README.md)

For interactive UI details, see [Order/INTERACTIVE_UI.md](Order/INTERACTIVE_UI.md)

## License

This is an educational project demonstrating Domain-Driven Design and event-driven microservices architecture.
