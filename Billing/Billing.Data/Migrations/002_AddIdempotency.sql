-- Add ProcessedMessages table for idempotency
CREATE TABLE ProcessedMessages (
    MessageId NVARCHAR(255) PRIMARY KEY,
    ProcessedAt DATETIME2 NOT NULL,
    ProcessorName NVARCHAR(100) NOT NULL,
    INDEX IX_ProcessedMessages_ProcessedAt (ProcessedAt)
);
