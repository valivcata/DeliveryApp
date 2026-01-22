# Quick Start Guide

## Get Started in 3 Steps

### Step 1: Configure Services (Already Done!)

All services are pre-configured with Azure SQL Database and Azure Service Bus. Configuration files are ready.

### Step 2: Run Database Migrations (If not already done)

Run these commands from the `DeliveryApp` directory:

```bash
# Order Service
cd Order
dotnet ef database update --project Order.Data
cd ..

# Billing Service
cd Billing
dotnet ef database update --project Billing.Data
cd ..

# Delivery Service
cd Delivery
dotnet ef database update --project Delivery.Data
cd ..
```

The seed data migrations will automatically populate the tables with sample restaurants and data.

### Step 3: Start All Services

Open three separate terminals and run each service:

**Terminal 1:**
```bash
cd Order
dotnet run
```

**Terminal 2:**
```bash
cd Billing
dotnet run
```

**Terminal 3:**
```bash
cd Delivery
dotnet run
```

## Test the Application

### 1. Open Swagger UI

Navigate to: **http://localhost:5002**

The Swagger UI will show all available endpoints.

### 2. Place an Order

Use the Swagger UI or send a POST request:

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

### 3. Watch the Magic Happen! ✨

1. **Order Service** processes your order and publishes to `order-topic`
2. **Billing Service** (check its console) receives the event and issues an invoice
3. **Delivery Service** (check its console) receives the invoice event, assigns a driver, and starts delivery

All three services work in parallel automatically!

## Available Test Restaurants

| ID | Name | Cuisine | Min Order |
|----|------|---------|-----------|
| REST-0001 | Pizza Palace | Italian | $15.00 |
| REST-0002 | Sushi Express | Japanese | $20.00 |
| REST-0003 | Burger Heaven | American | $10.00 |
| REST-0004 | Thai Delight | Thai | $18.00 |
| REST-0005 | Mexican Fiesta | Mexican | $12.00 |

## Sample API Requests

### Place Order for Pizza
```json
{
  "restaurantId": "REST-0001",
  "customerPhone": "0700111222",
  "deliveryAddress": "Bd. Unirii 1, Bucharest",
  "orderAmount": 35.00
}
```

### Place Order for Sushi
```json
{
  "restaurantId": "REST-0002",
  "customerPhone": "0700333444",
  "deliveryAddress": "Str. Franceza 12, Bucharest",
  "orderAmount": 55.00
}
```

### Place Order for Burgers
```json
{
  "restaurantId": "REST-0003",
  "customerPhone": "0700555666",
  "deliveryAddress": "Calea Victoriei 25, Bucharest",
  "orderAmount": 22.00
}
```

## Verify Event Flow

After placing an order, check the console output of each service:

1. **Order Service Console**: Shows order validation, enrichment, and placement
2. **Billing Service Console**: Shows invoice calculation, tax validation, and issuance
3. **Delivery Service Console**: Shows driver assignment, route optimization, and delivery start

## Health Check

```bash
curl http://localhost:5002/api/orders/health
```

Should return:
```json
{
  "status": "Healthy",
  "service": "Order API",
  "timestamp": "2026-01-22T..."
}
```

## Stop All Services

Press `Ctrl+C` in each terminal where you started the services to gracefully shut them down.

## Need More Help?

See the full [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for detailed documentation.

## Architecture Highlights

✅ **3+ Operations per Workflow** - Each service has multiple domain operations
✅ **REST API with Swagger** - Order service exposes a fully documented API
✅ **Event-Driven Communication** - Services communicate via Azure Service Bus
✅ **Parallel Processing** - All services run independently and concurrently
✅ **Idempotency** - Duplicate messages are automatically handled
✅ **Pre-populated Data** - Sample restaurants and test data ready to use

## Troubleshooting

**Services won't start?**
- Make sure you're in the `DeliveryApp` directory
- Check that .NET 9 SDK is installed: `dotnet --version`

**Database connection errors?**
- Connection strings are pre-configured for Azure SQL
- If you see connection errors, the databases might need to be created

**No events being processed?**
- Make sure all three services are running
- Check that Azure Service Bus topics exist: `order-topic`, `billing-topic`, `delivery-topic`
- **Important**: Each subscription must have a TrueFilter rule (not just a default $Default rule)
- Verify subscription rules in Azure Portal: Service Bus > Topics > Subscriptions > Rules
- If rules are missing, messages will be discarded silently

**Phone number validation error?**
- Phone must be exactly 10 digits (e.g., `0712345678`)
- Do not include country prefix like `+40`

**Port 5002 already in use?**
- Stop other .NET applications: `pkill -f "Order/bin"`
- Or change the port in `Properties/launchSettings.json`

**Billing/Delivery not receiving messages?**
- Ensure Azure Service Bus subscriptions have a TrueFilter rule
- Check that all three services are running simultaneously
