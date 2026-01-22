-- Seed sample data for Delivery service
-- This script is safe to run multiple times

-- Insert sample deliveries (optional - for testing)
-- Note: In production, deliveries are created automatically from invoice events
IF NOT EXISTS (SELECT 1 FROM Deliveries WHERE RestaurantId = 'REST-0001' AND CustomerPhone = '+40700000001')
BEGIN
    INSERT INTO Deliveries (Id, RestaurantId, CustomerPhone, DeliveryAddress, DriverId, Route, Status, CreatedAt, StartedAt)
    VALUES
        (NEWID(), 'REST-0001', '+40700000001', 'Str. Academiei 14, Bucharest, Romania', 'DRV-001', 'Route A via Str. Batiștei', 'Started', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'REST-0002', '+40700000002', 'Bd. Carol I 11, Bucharest, Romania', 'DRV-002', 'Route B via Bd. Regina Elisabeta', 'Started', DATEADD(MINUTE, -20, GETUTCDATE()), DATEADD(MINUTE, -20, GETUTCDATE())),
        (NEWID(), 'REST-0003', '+40700000003', 'Str. Universitatii 13, Bucharest, Romania', 'DRV-003', 'Route C via Str. Doamnei', 'Started', DATEADD(MINUTE, -50, GETUTCDATE()), DATEADD(MINUTE, -50, GETUTCDATE())),
        (NEWID(), 'REST-0004', '+40700000004', 'Calea Victoriei 120, Bucharest, Romania', 'DRV-004', 'Route D via Calea Dorobanților', 'Started', DATEADD(HOUR, -1, DATEADD(MINUTE, -50, GETUTCDATE())), DATEADD(HOUR, -1, DATEADD(MINUTE, -50, GETUTCDATE()))),
        (NEWID(), 'REST-0005', '+40700000005', 'Str. Ion Campineanu 10, Bucharest, Romania', 'DRV-005', 'Route E via Bd. Magheru', 'Started', DATEADD(HOUR, -2, DATEADD(MINUTE, -50, GETUTCDATE())), DATEADD(HOUR, -2, DATEADD(MINUTE, -50, GETUTCDATE())));
END

PRINT 'Delivery seed data completed successfully'
