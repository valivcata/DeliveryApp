-- Add Restaurants table
CREATE TABLE Restaurants (
    RestaurantId NVARCHAR(50) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Cuisine NVARCHAR(100) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    MinimumOrder DECIMAL(18,2) NOT NULL,
    DeliveryFee DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);

-- Create indexes
CREATE INDEX IX_Restaurants_Name ON Restaurants(Name);
CREATE INDEX IX_Restaurants_Status ON Restaurants(Status);

-- Insert sample restaurants
INSERT INTO Restaurants (RestaurantId, Name, Cuisine, Address, MinimumOrder, DeliveryFee, Status, CreatedAt)
VALUES
    ('REST-0001', 'Pizza Palace', 'Italian', '123 Main St, Downtown', 15.00, 3.99, 'Active', GETUTCDATE()),
    ('REST-0002', 'Sushi Express', 'Japanese', '456 Oak Ave, Midtown', 20.00, 4.99, 'Active', GETUTCDATE()),
    ('REST-0003', 'Burger Heaven', 'American', '789 Elm St, Uptown', 10.00, 2.99, 'Active', GETUTCDATE()),
    ('REST-0004', 'Thai Delight', 'Thai', '321 Pine Rd, Eastside', 18.00, 3.49, 'Active', GETUTCDATE()),
    ('REST-0005', 'Mexican Fiesta', 'Mexican', '654 Maple Dr, Westside', 12.00, 3.99, 'Active', GETUTCDATE()),
    ('REST-0006', 'Chinese Dragon', 'Chinese', '987 Cedar Ln, Southside', 16.00, 4.49, 'Active', GETUTCDATE()),
    ('REST-0007', 'Indian Spice', 'Indian', '147 Birch Way, Northside', 17.00, 3.99, 'Active', GETUTCDATE()),
    ('REST-0008', 'Greek Taverna', 'Greek', '258 Walnut Blvd, Central', 14.00, 3.49, 'Active', GETUTCDATE());
