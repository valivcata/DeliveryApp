-- Add ProcessedMessages table for idempotency (idempotent)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessedMessages')
BEGIN
    CREATE TABLE ProcessedMessages (
        MessageId NVARCHAR(255) PRIMARY KEY,
        ProcessedAt DATETIME2 NOT NULL,
        ProcessorName NVARCHAR(100) NOT NULL,
        INDEX IX_ProcessedMessages_ProcessedAt (ProcessedAt)
    );
    
    PRINT 'ProcessedMessages table created successfully';
END
ELSE
BEGIN
    PRINT 'ProcessedMessages table already exists - skipping creation';
END
