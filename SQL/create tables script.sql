-- Switch to ProjectDB (if not already selected)
USE ProjectDB;
GO

-- Create Employee table
CREATE TABLE employees (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL
);
GO

-- Create Product table
CREATE TABLE products (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(200) NOT NULL,
    price FLOAT NOT NULL,
    image NVARCHAR(500) NOT NULL,
    description NVARCHAR(MAX) NULL
);
GO

-- Create CartItem table
CREATE TABLE cartItems (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT NOT NULL,
    quantity FLOAT NOT NULL DEFAULT 1,
    CONSTRAINT FK_CartItem_Product FOREIGN KEY (product_id) 
        REFERENCES products(id) ON DELETE CASCADE
);
GO

-- Add indexes for better performance
CREATE INDEX IX_CartItems_ProductId ON cartItems(product_id);
GO