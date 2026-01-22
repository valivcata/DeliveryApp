# DeliveryApp - Food Delivery Management System

A microservices-based food delivery application built with .NET 9, demonstrating event-driven architecture, CQRS patterns, and Azure Service Bus integration.

## Architecture Overview

The application consists of three microservices that communicate asynchronously via Azure Service Bus:

1. **Order Service** (Web API) - Handles order placement and management
2. **Billing Service** (Worker) - Processes orders and issues invoices
3. **Delivery Service** (Worker) - Manages delivery assignments and routing

### Event Flow

```
Order API (POST /api/orders)
    ↓
Order Placed Event → order-topic
    ↓
Billing Service (Listener) → Processes Order
    ↓
Invoice Issued Event → billing-topic
    ↓
Delivery Service (Listener) → Assigns Driver & Starts Delivery
    ↓
Delivery Started Event → delivery-topic
```

## Workflow Operations

Each service implements domain-driven design with multiple operations per workflow:

### Order Workflow (3+ operations)
1. **ValidateOrderOperation** - Validates all order fields
2. **EnrichOrderOperation** - Adds metadata (order date, delivery time, reference)
3. **PlaceOrderOperation** - Completes order placement

### Billing Workflow (3+ operations)
1. **CalculateInvoiceOperation** - Calculates amounts and tax
2. **ValidateTaxOperation** - Validates tax compliance
3. **IssueInvoiceOperation** - Issues the final invoice

### Delivery Workflow (3+ operations)
1. **AssignDeliveryOperation** - Assigns driver and validates destination
2. **OptimizeRouteOperation** - Calculates optimal route, distance, and time
3. **StartDeliveryOperation** - Starts the delivery process

## Prerequisites

- .NET 9 SDK
- SQL Server (Azure SQL Database configured)
- Azure Service Bus namespace
- Visual Studio 2022 or VS Code

## Configuration

Each service has an `appsettings.json` with the following structure:

```json
{
  "ConnectionStrings": {
    "OrderDb": "Your SQL Server connection string"
  },
  "ServiceBus": {
    "ConnectionString": "Your Service Bus connection string",
    "TopicName": "topic-name",
    "SubscriptionName": "subscription-name",
    "OutputTopicName": "output-topic-name"
  }
}
```

## Database Setup

### 1. Run Migrations

Each service has its own database. Migrations are already created.

**Order Service:**
```bash
cd Order
dotnet ef database update --project Order.Data
```

**Billing Service:**
```bash
cd Billing
dotnet ef database update --project Billing.Data
```

**Delivery Service:**
```bash
cd Delivery
dotnet ef database update --project Delivery.Data
```

### 2. Run SQL Seed Scripts

Execute the SQL migration files in order for each service:

**Order Service:**
- `001_AddRestaurants.sql` - Creates restaurants table and seeds 8 restaurants
- `002_AddIdempotency.sql` - Adds message idempotency support
- `003_SeedData.sql` - Seeds additional restaurants and sample orders

**Billing Service:**
- `002_AddIdempotency.sql` - Adds message idempotency support
- `003_SeedData.sql` - Seeds sample invoices

**Delivery Service:**
- `002_AddIdempotency.sql` - Adds message idempotency support
- `003_SeedData.sql` - Seeds sample deliveries

## Running the Application

### Start All Services

You need to run all three services in parallel. Open three separate terminal windows:

**Terminal 1 - Order API:**
```bash
cd Order
dotnet run
```

The Order API will start at `http://localhost:5002` (or check console output)
Swagger UI is available at the root URL: `http://localhost:5002`

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

## Using the API

### Swagger UI

Navigate to `http://localhost:5002` in your browser to access the Swagger UI.

### Place an Order via POST Request

**Endpoint:** `POST /api/orders`

**Request Body:**
```json
{
  "restaurantId": "REST-0001",
  "customerPhone": "0712345678",
  "deliveryAddress": "Str. Academiei 14, Bucharest, Romania",
  "orderAmount": 45.50
}
```

**Example using curl:**
```bash
curl -X POST "http://localhost:5002/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "restaurantId": "REST-0001",
    "customerPhone": "0712345678",
    "deliveryAddress": "Str. Academiei 14, Bucharest, Romania",
    "orderAmount": 45.50
  }'
```

**Example using PowerShell:**
```powershell
$body = @{
    restaurantId = "REST-0001"
    customerPhone = "0712345678"
    deliveryAddress = "Str. Academiei 14, Bucharest, Romania"
    orderAmount = 45.50
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/orders" `
  -ContentType "application/json" -Body $body
```

### Available Restaurants

The database is seeded with the following restaurants:

| Restaurant ID | Name | Cuisine | Min Order | Delivery Fee |
|--------------|------|---------|-----------|--------------|
| REST-0001 | Pizza Palace | Italian | $15.00 | $3.99 |
| REST-0002 | Sushi Express | Japanese | $20.00 | $4.99 |
| REST-0003 | Burger Heaven | American | $10.00 | $2.99 |
| REST-0004 | Thai Delight | Thai | $18.00 | $3.49 |
| REST-0005 | Mexican Fiesta | Mexican | $12.00 | $3.99 |
| REST-0006 | Chinese Dragon | Chinese | $16.00 | $4.49 |
| REST-0007 | Indian Spice | Indian | $17.00 | $3.99 |
| REST-0008 | Greek Taverna | Greek | $14.00 | $3.49 |

## Event-Driven Communication

When you place an order via the API:

1. **Order Service** validates and processes the order
2. **Event Published** to `order-topic` in Azure Service Bus
3. **Billing Service** listens to `order-topic`, receives the event, and issues an invoice
4. **Event Published** to `billing-topic`
5. **Delivery Service** listens to `billing-topic`, receives the event, assigns a driver, and starts delivery
6. **Event Published** to `delivery-topic`

All services run in parallel and process events asynchronously.

## Idempotency

All services implement idempotency to prevent duplicate processing:
- Each message has a unique `MessageId`
- Services check `ProcessedMessages` table before processing
- Duplicate messages are acknowledged and skipped

## Health Check

Check if the Order API is running:

**Endpoint:** `GET /api/orders/health`

```bash
curl http://localhost:5000/api/orders/health
```

## Project Structure

```
DeliveryApp/
├── Order/                      # Web API service
│   ├── Controllers/            # API controllers
│   ├── Order.Domain/          # Business logic
│   │   ├── Models/
│   │   ├── Operations/        # Domain operations
│   │   └── Workflows/         # Business workflows
│   ├── Order.Data/            # Data access layer
│   │   ├── Migrations/        # EF Core migrations
│   │   └── Repositories/
│   └── Order.Events.ServiceBus/ # Event publishing
├── Billing/                    # Worker service
│   ├── Workers/               # Background listeners
│   ├── Billing.Domain/        # Business logic
│   └── Billing.Data/          # Data access layer
└── Delivery/                   # Worker service
    ├── Workers/               # Background listeners
    ├── Delivery.Domain/       # Business logic
    └── Delivery.Data/         # Data access layer
```

## Technologies Used

- **.NET 9** - Framework
- **ASP.NET Core Web API** - Order service
- **Entity Framework Core 9** - ORM
- **Azure SQL Database** - Data storage
- **Azure Service Bus** - Message broker
- **Swashbuckle** - Swagger/OpenAPI documentation
- **Domain-Driven Design** - Architecture pattern
- **CQRS** - Command Query Responsibility Segregation

## Development

### Adding New Operations

To add a new operation to a workflow:

1. Create a new operation class in the `Operations` folder
2. Inherit from the domain operation base class
3. Override the appropriate state handler method
4. Add the operation to the workflow's `ExecuteBusinessLogic` method

### Adding New Domain States

1. Add the new state to the domain model
2. Update the operation base class to handle the new state
3. Update existing operations as needed

## Troubleshooting

### Service Bus Connection Issues
- Verify the Service Bus connection string in `appsettings.json`
- Ensure topics and subscriptions are created in Azure
- Check that the Service Bus namespace has proper access policies

### Database Connection Issues
- Verify SQL Server connection strings
- Ensure databases are created
- Run all migrations in order

### No Events Being Processed
- Check that all three services are running
- Verify Service Bus topics and subscriptions exist
- Check application logs for errors

## License

This project is for educational purposes as part of the PSSC (Proiectarea Sistemelor Software Complexe) course.
