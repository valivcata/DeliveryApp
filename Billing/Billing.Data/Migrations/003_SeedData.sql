-- Seed sample data for Billing service
-- This script is safe to run multiple times

-- Insert sample invoices (optional - for testing)
-- Note: In production, invoices are created automatically from order events
IF NOT EXISTS (SELECT 1 FROM Invoices WHERE RestaurantId = 'REST-0001' AND CustomerPhone = '+40700000001')
BEGIN
    INSERT INTO Invoices (Id, RestaurantId, CustomerPhone, Amount, Tax, Total, Status, CreatedAt, IssuedAt)
    VALUES
        (NEWID(), 'REST-0001', '+40700000001', 45.50, 8.65, 54.15, 'Issued', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'REST-0002', '+40700000002', 67.80, 12.88, 80.68, 'Issued', DATEADD(MINUTE, -25, GETUTCDATE()), DATEADD(MINUTE, -25, GETUTCDATE())),
        (NEWID(), 'REST-0003', '+40700000003', 32.00, 6.08, 38.08, 'Issued', DATEADD(MINUTE, -55, GETUTCDATE()), DATEADD(MINUTE, -55, GETUTCDATE())),
        (NEWID(), 'REST-0004', '+40700000004', 54.20, 10.30, 64.50, 'Issued', DATEADD(HOUR, -1, DATEADD(MINUTE, -55, GETUTCDATE())), DATEADD(HOUR, -1, DATEADD(MINUTE, -55, GETUTCDATE()))),
        (NEWID(), 'REST-0005', '+40700000005', 41.90, 7.96, 49.86, 'Issued', DATEADD(HOUR, -2, DATEADD(MINUTE, -55, GETUTCDATE())), DATEADD(HOUR, -2, DATEADD(MINUTE, -55, GETUTCDATE())));
END

PRINT 'Billing seed data completed successfully'
