-- Seed sample data for Order service
-- This script is safe to run multiple times (uses MERGE for upsert)

-- Ensure restaurants exist (already created in 001_AddRestaurants.sql but this adds more)
IF NOT EXISTS (SELECT 1 FROM Restaurants WHERE RestaurantId = 'REST-0009')
BEGIN
    INSERT INTO Restaurants (RestaurantId, Name, Cuisine, Address, MinimumOrder, DeliveryFee, Status, CreatedAt)
    VALUES
        ('REST-0009', 'French Bistro', 'French', '369 Rose St, Downtown', 25.00, 5.99, 'Active', GETUTCDATE()),
        ('REST-0010', 'Korean BBQ', 'Korean', '741 Lily Ave, Midtown', 22.00, 4.49, 'Active', GETUTCDATE());
END

-- Insert sample orders (optional - for testing)
-- Note: In production, orders are created via API, but this helps with initial testing
IF NOT EXISTS (SELECT 1 FROM Orders WHERE RestaurantId = 'REST-0001' AND CustomerPhone = '+40700000001')
BEGIN
    INSERT INTO Orders (Id, RestaurantId, CustomerPhone, DeliveryAddress, OrderAmount, Status, CreatedAt, PlacedAt)
    VALUES
        (NEWID(), 'REST-0001', '+40700000001', 'Str. Academiei 14, Bucharest, Romania', 45.50, 'Placed', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'REST-0002', '+40700000002', 'Bd. Carol I 11, Bucharest, Romania', 67.80, 'Placed', DATEADD(MINUTE, -30, GETUTCDATE()), DATEADD(MINUTE, -30, GETUTCDATE())),
        (NEWID(), 'REST-0003', '+40700000003', 'Str. Universitatii 13, Bucharest, Romania', 32.00, 'Placed', DATEADD(HOUR, -1, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE())),
        (NEWID(), 'REST-0004', '+40700000004', 'Calea Victoriei 120, Bucharest, Romania', 54.20, 'Placed', DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE())),
        (NEWID(), 'REST-0005', '+40700000005', 'Str. Ion Campineanu 10, Bucharest, Romania', 41.90, 'Placed', DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE()));
END

PRINT 'Order seed data completed successfully'
