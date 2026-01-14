# Order Service - Interactive Console UI

## Overview
The Order service now includes an interactive console UI for placing orders, similar to the Booking service in Proiect_PSSC/app1.

## Features

### Interactive Restaurant Selection
- **Arrow Key Navigation**: Use ↑/↓ arrow keys to navigate through available restaurants
- **Visual Highlighting**: Selected restaurant is highlighted with inverted colors (black text on white background)
- **Real-time Display**: Shows restaurant details including:
  - Restaurant ID
  - Restaurant Name
  - Cuisine Type
  - Minimum Order Amount
  - Delivery Fee
  
### Order Placement Flow
1. **Select Restaurant**: Navigate and choose from active restaurants
2. **Enter Customer Phone**: Provide contact information
3. **Enter Delivery Address**: Specify delivery location
4. **Enter Order Amount**: Provide the total order value

### Visual Feedback
- **Success**: Green text confirmation with order details
- **Failure**: Red text with error message
- **Structured Layout**: Clean dividers and formatting for easy reading

## Usage

### Running the Service
```bash
cd DeliveryApp/Order
dotnet run
```

### Database Setup
Before running, ensure you've created the Restaurants table:

```bash
# Run the migration script in your SQL Server database
sqlcmd -S localhost -d OrderDb -i Migrations/001_AddRestaurants.sql
```

Or manually create the table and insert sample data from [001_AddRestaurants.sql](Migrations/001_AddRestaurants.sql).

### Sample Restaurants
The migration includes 8 sample restaurants:
- Pizza Palace (Italian)
- Sushi Express (Japanese)
- Burger Heaven (American)
- Thai Delight (Thai)
- Mexican Fiesta (Mexican)
- Chinese Dragon (Chinese)
- Indian Spice (Indian)
- Greek Taverna (Greek)

## Controls
- **↑/↓ Arrow Keys**: Navigate restaurant list
- **Enter**: Confirm selection
- **Esc**: Exit the application

## Technical Details

### New Components
- `OrderRequestListener.cs`: Background service with interactive console UI
- `RestaurantEntity.cs`: Database model for restaurants
- Updated `OrderDbContext.cs`: Added Restaurants DbSet and configuration
- Updated `Program.cs`: Registered the OrderRequestListener as a hosted service

### How It Works
1. The `OrderRequestListener` runs as a `BackgroundService`
2. Fetches active restaurants from the database
3. Displays them in an interactive list using console manipulation
4. Processes user input and validates data
5. Executes the `PlaceOrderWorkflow` with the entered data
6. Publishes events to Azure Service Bus on success

## Example Session

```
============================================================
Place a New Order (or 'Esc' to quit):
============================================================

Use ↑/↓ arrow keys to select, Enter to confirm, Esc to exit:
------------------------------------------------------------
→ REST001    Pizza Palace              [Italian        ] Min: $15.00  Fee: $3.99
  REST002    Sushi Express             [Japanese       ] Min: $20.00  Fee: $4.99
  REST003    Burger Heaven             [American       ] Min: $10.00  Fee: $2.99
  REST004    Thai Delight              [Thai           ] Min: $18.00  Fee: $3.49
------------------------------------------------------------

Customer Phone: +1-555-1234
------------------------------------------------------------
Delivery Details:
------------------------------------------------------------
Delivery Address: 123 Main Street, Apt 4B
Order Amount ($): 45.50

------------------------------------------------------------
Result: OrderPlacedEvent
Success! Order placed at 1/14/2026 3:45:23 PM
Restaurant: REST001, Amount: $45.50
Delivery to: 123 Main Street, Apt 4B
------------------------------------------------------------
```

## Architecture Benefits
- **Separation of Concerns**: UI logic separated from business logic
- **Testability**: Workflow remains independently testable
- **User Experience**: Interactive selection is more intuitive than typing IDs
- **Error Prevention**: Visual selection reduces typos in restaurant IDs
