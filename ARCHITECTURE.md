# Architecture Diagram

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Food Delivery Application                            │
│                    Event-Driven Microservices Architecture                   │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                               │
│                              User / Client                                    │
│                                                                               │
│                      HTTP POST /api/orders                                    │
│                         (JSON Payload)                                        │
│                                                                               │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ORDER SERVICE (Web API)                              │
│                         Port: http://localhost:5002                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  Controllers/                                                                 │
│  └── OrdersController                                                         │
│      ├── POST /api/orders          ← Place Order Endpoint                   │
│      └── GET /api/orders/health    ← Health Check                           │
│                                                                               │
│  Workflow: PlaceOrderWorkflow                                                │
│  └── Operations:                                                             │
│      1. ValidateOrderOperation     → Validate all fields                    │
│      2. EnrichOrderOperation       → Add metadata, timestamps, reference    │
│      3. PlaceOrderOperation        → Complete order placement               │
│                                                                               │
│  Models:                                                                     │
│  UnvalidatedOrder → ValidatedOrder → EnrichedOrder → OrderPlaced            │
│                                                                               │
│  Database: OrderDb (Azure SQL)                                               │
│  └── Tables: Orders, Restaurants, ProcessedMessages                         │
│                                                                               │
│  Events Published:                                                           │
│  └── OrderPlaced → order-topic                                              │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ Azure Service Bus
                                 │ Topic: order-topic
                                 │ Subscription: billing-subscription
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        BILLING SERVICE (Worker)                               │
│                          Background Service                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  Workers/                                                                     │
│  └── OrderPlacedListener (BackgroundService)                                │
│      └── Listens to order-topic                                             │
│                                                                               │
│  Workflow: IssueInvoiceWorkflow                                              │
│  └── Operations:                                                             │
│      1. CalculateInvoiceOperation  → Calculate amount, tax (19%), total     │
│      2. ValidateTaxOperation       → Validate tax compliance (0-30%)        │
│      3. IssueInvoiceOperation      → Issue final invoice                    │
│                                                                               │
│  Models:                                                                     │
│  UnprocessedInvoice → CalculatedInvoice → ValidatedInvoice → InvoiceIssued  │
│                                                                               │
│  Database: BillingDb (Azure SQL)                                             │
│  └── Tables: Invoices, ProcessedMessages                                    │
│                                                                               │
│  Events Published:                                                           │
│  └── InvoiceIssued → billing-topic                                          │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 │ Azure Service Bus
                                 │ Topic: billing-topic
                                 │ Subscription: delivery-subscription
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       DELIVERY SERVICE (Worker)                               │
│                          Background Service                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  Workers/                                                                     │
│  └── InvoiceIssuedListener (BackgroundService)                              │
│      └── Listens to billing-topic                                           │
│                                                                               │
│  Workflow: StartDeliveryWorkflow                                             │
│  └── Operations:                                                             │
│      1. AssignDeliveryOperation    → Assign driver, validate destination    │
│      2. OptimizeRouteOperation     → Calculate route, distance, time        │
│      3. StartDeliveryOperation     → Start delivery process                 │
│                                                                               │
│  Models:                                                                     │
│  RequestedDelivery → AssignedDelivery → OptimizedDelivery → DeliveryStarted │
│                                                                               │
│  Database: DeliveryDb (Azure SQL)                                            │
│  └── Tables: Deliveries, ProcessedMessages                                  │
│                                                                               │
│  Events Published:                                                           │
│  └── DeliveryStarted → delivery-topic                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```
1. Client Request
   │
   ├─→ POST /api/orders
   │   {
   │     "restaurantId": "REST-0001",
   │     "customerPhone": "0712345678",
   │     "deliveryAddress": "Str. Academiei 14",
   │     "orderAmount": 45.50
   │   }
   │
   ▼

2. Order Service Processing
   │
   ├─→ ValidateOrderOperation
   │   ├── Validate restaurant ID (must exist)
   │   ├── Validate phone format (exactly 10 digits)
   │   ├── Validate address (non-empty, max 500 chars)
   │   └── Validate amount (> 0)
   │
   ├─→ EnrichOrderOperation
   │   ├── Add order date (UTC now)
   │   ├── Calculate estimated delivery (30-60 min)
   │   └── Generate order reference (ORD-20260122-ABC12345)
   │
   ├─→ PlaceOrderOperation
   │   └── Mark order as placed with timestamp
   │
   ├─→ Save to OrderDb
   │
   └─→ Publish Event to order-topic
       {
         "RestaurantId": "REST-0001",
         "CustomerPhone": "0712345678",
         "DeliveryAddress": "Str. Academiei 14",
         "OrderAmount": 45.50,
         "PlacedAt": "2026-01-22T12:34:56Z"
       }
   │
   ▼

3. Billing Service Processing
   │
   ├─→ Receive from order-topic
   │
   ├─→ Check idempotency (ProcessedMessages table)
   │
   ├─→ CalculateInvoiceOperation
   │   ├── Amount: 45.50
   │   ├── Tax (10%): 4.55
   │   └── Total: 50.05
   │
   ├─→ ValidateTaxOperation
   │   └── Verify tax rate is 0-30% ✓
   │
   ├─→ IssueInvoiceOperation
   │   └── Mark invoice as issued
   │
   ├─→ Save to BillingDb
   │
   ├─→ Mark message as processed (idempotency)
   │
   └─→ Publish Event to billing-topic
       {
         "RestaurantId": "REST-0001",
         "CustomerPhone": "0712345678",
         "DeliveryAddress": "Str. Academiei 14",
         "Amount": 45.50,
         "Tax": 4.55,
         "Total": 50.05,
         "IssuedAt": "2026-01-22T12:34:57Z"
       }
   │
   ▼

4. Delivery Service Processing
   │
   ├─→ Receive from billing-topic
   │
   ├─→ Check idempotency (ProcessedMessages table)
   │
   ├─→ AssignDeliveryOperation
   │   ├── Validate destination
   │   ├── Assign random driver (DRV-001, DRV-002, etc.)
   │   └── Calculate initial route
   │
   ├─→ OptimizeRouteOperation
   │   ├── Calculate distance: 5.43 km
   │   └── Estimate time: ~11 minutes (30 km/h avg)
   │
   ├─→ StartDeliveryOperation
   │   └── Mark delivery as started
   │
   ├─→ Save to DeliveryDb
   │
   ├─→ Mark message as processed (idempotency)
   │
   └─→ Publish Event to delivery-topic
       {
         "RestaurantId": "REST-0001",
         "CustomerPhone": "0712345678",
         "DriverId": "DRV-001",
         "Route": "Route A via Str. Batiștei to Str. Academiei 14",
         "StartedAt": "2026-01-22T12:34:58Z"
       }
```

## Technology Stack

```
┌────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
├────────────────────────────────────────────────────────────┤
│  • ASP.NET Core Web API (Order Service)                    │
│  • Swagger/OpenAPI (API Documentation)                     │
│  • REST Controllers                                        │
└────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├────────────────────────────────────────────────────────────┤
│  • Background Workers (Billing, Delivery)                  │
│  • Event Listeners (Azure Service Bus)                    │
│  • Workflow Orchestration                                 │
└────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────┐
│                      Domain Layer                           │
├────────────────────────────────────────────────────────────┤
│  • Domain Models (DDD Aggregates)                          │
│  • Value Objects                                           │
│  • Domain Operations                                       │
│  • Business Workflows                                      │
└────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
├────────────────────────────────────────────────────────────┤
│  • Entity Framework Core 9.0                               │
│  • Azure SQL Database                                      │
│  • Azure Service Bus                                       │
│  • Repository Pattern                                      │
└────────────────────────────────────────────────────────────┘
```

## Design Patterns Used

1. **Domain-Driven Design (DDD)**
   - Bounded Contexts (Order, Billing, Delivery)
   - Aggregates and Value Objects
   - Domain Events

2. **CQRS (Command Query Responsibility Segregation)**
   - Commands: PlaceOrderDto, OrderPlacedMessageDto, etc.
   - Events: OrderPlacedEvent, InvoiceIssuedEvent, etc.

3. **Event-Driven Architecture**
   - Asynchronous communication via Service Bus
   - Publish-Subscribe pattern
   - Event sourcing for audit trail

4. **Repository Pattern**
   - IOrderRepository, IInvoiceRepository, IDeliveryRepository
   - Separation of data access from business logic

5. **Strategy Pattern**
   - Different operations for different states
   - Pluggable operation implementations

6. **Chain of Responsibility**
   - Operations chained in workflows
   - State transformations

## Scalability Considerations

```
Load Balancer
     │
     ├─→ Order API Instance 1 ─┐
     ├─→ Order API Instance 2 ─┤
     └─→ Order API Instance 3 ─┤
                                ├─→ Azure Service Bus
                                │   (order-topic)
                                │
     ┌──────────────────────────┘
     │
     ├─→ Billing Worker 1 ─┐
     ├─→ Billing Worker 2 ─┤
     └─→ Billing Worker 3 ─┤
                            ├─→ Azure Service Bus
                            │   (billing-topic)
                            │
     ┌──────────────────────┘
     │
     ├─→ Delivery Worker 1
     ├─→ Delivery Worker 2
     └─→ Delivery Worker 3
```

**Benefits:**
- Horizontal scaling of each service independently
- Service Bus handles load distribution
- Database per service (independent scaling)
- Idempotency prevents duplicate processing
- Event replay capability for resilience

## Monitoring & Observability

Each service logs:
- Request/Response data
- Event processing
- Error conditions
- Performance metrics

**Recommended additions:**
- Application Insights for distributed tracing
- Health check endpoints
- Prometheus metrics
- ELK stack for centralized logging
