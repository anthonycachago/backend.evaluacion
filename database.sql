/* =====================================================================
   Inventory - Script de creación de base de datos (SQL Server)

   Arquitectura de microservicios: cada servicio es dueño de su propia
   base de datos (patrón Database per Service).
     - InventoryProductsDb      -> Microservicio de Productos
     - InventoryTransactionsDb  -> Microservicio de Transacciones

   La relación Transactions.ProductId -> Products.Id es lógica (no hay FK
   entre bases); la integridad la garantiza el servicio de Transacciones
   validando el producto contra la API de Productos.

   Ejecución: SSMS / Azure Data Studio, o
     sqlcmd -S localhost\SQLEXPRESS -E -i database.sql
   ===================================================================== */

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------
   1. Base de datos de Productos
   --------------------------------------------------------------------- */
IF DB_ID(N'InventoryProductsDb') IS NULL
    CREATE DATABASE InventoryProductsDb;
GO

USE InventoryProductsDb;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id          UNIQUEIDENTIFIER NOT NULL,
        Name        NVARCHAR(150)    NOT NULL,
        Description NVARCHAR(1000)   NULL,
        Category    NVARCHAR(100)    NOT NULL,
        ImageUrl    NVARCHAR(500)    NULL,
        Price       DECIMAL(18, 2)   NOT NULL,
        Stock       INT              NOT NULL,
        IsDeleted   BIT              NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT (0),
        CreatedAt   DATETIME2(3)     NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt   DATETIME2(3)     NULL,

        CONSTRAINT PK_Products PRIMARY KEY (Id),
        CONSTRAINT CK_Products_Price CHECK (Price > 0),
        CONSTRAINT CK_Products_Stock CHECK (Stock >= 0)
    );

    CREATE INDEX IX_Products_Name ON dbo.Products (Name);
    CREATE INDEX IX_Products_Category ON dbo.Products (Category);
END
GO

/* Datos de ejemplo (solo si la tabla está vacía) */
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (Id, Name, Description, Category, ImageUrl, Price, Stock)
    VALUES
        (NEWID(), N'Laptop Lenovo ThinkPad E14', N'Intel Core i5, 16 GB RAM, SSD 512 GB', N'Computadoras', N'https://picsum.photos/seed/laptop/400/300', 899.99, 15),
        (NEWID(), N'Monitor Samsung 27"', N'Monitor IPS 27 pulgadas Full HD 75 Hz', N'Monitores', N'https://picsum.photos/seed/monitor/400/300', 219.50, 25),
        (NEWID(), N'Teclado mecánico Redragon', N'Teclado mecánico switches rojos, retroiluminado', N'Accesorios', N'https://picsum.photos/seed/keyboard/400/300', 59.90, 40),
        (NEWID(), N'Mouse Logitech M185', N'Mouse inalámbrico USB', N'Accesorios', N'https://picsum.photos/seed/mouse/400/300', 18.75, 60),
        (NEWID(), N'Impresora Epson L3250', N'Multifunción tanque de tinta Wi-Fi', N'Impresoras', N'https://picsum.photos/seed/printer/400/300', 249.00, 8),
        (NEWID(), N'Disco SSD Kingston 1 TB', N'SSD NVMe PCIe 4.0', N'Almacenamiento', N'https://picsum.photos/seed/ssd/400/300', 74.99, 30),
        (NEWID(), N'Memoria RAM Corsair 16 GB', N'DDR4 3200 MHz', N'Componentes', N'https://picsum.photos/seed/ram/400/300', 45.00, 50),
        (NEWID(), N'Audífonos HyperX Cloud II', N'Audífonos gamer con sonido 7.1', N'Audio', N'https://picsum.photos/seed/headset/400/300', 89.99, 12),
        (NEWID(), N'Webcam Logitech C920', N'Cámara web Full HD 1080p', N'Accesorios', N'https://picsum.photos/seed/webcam/400/300', 65.00, 20),
        (NEWID(), N'Router TP-Link Archer AX23', N'Router Wi-Fi 6 doble banda', N'Redes', N'https://picsum.photos/seed/router/400/300', 79.90, 18),
        (NEWID(), N'Tablet Samsung Galaxy Tab A9', N'Pantalla 11", 128 GB', N'Tablets', N'https://picsum.photos/seed/tablet/400/300', 229.00, 10),
        (NEWID(), N'Parlante JBL Flip 6', N'Parlante Bluetooth resistente al agua', N'Audio', N'https://picsum.photos/seed/speaker/400/300', 119.00, 14);
END
GO

/* ---------------------------------------------------------------------
   2. Base de datos de Transacciones
   --------------------------------------------------------------------- */
IF DB_ID(N'InventoryTransactionsDb') IS NULL
    CREATE DATABASE InventoryTransactionsDb;
GO

USE InventoryTransactionsDb;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions
    (
        Id          UNIQUEIDENTIFIER NOT NULL,
        [Date]      DATETIME2(3)     NOT NULL,
        [Type]      TINYINT          NOT NULL,   -- 1 = Compra (Purchase), 2 = Venta (Sale)
        ProductId   UNIQUEIDENTIFIER NOT NULL,   -- Referencia lógica a InventoryProductsDb.dbo.Products(Id)
        Quantity    INT              NOT NULL,
        UnitPrice   DECIMAL(18, 2)   NOT NULL,
        TotalPrice  DECIMAL(18, 2)   NOT NULL,
        Detail      NVARCHAR(500)    NULL,
        CreatedAt   DATETIME2(3)     NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt   DATETIME2(3)     NULL,

        CONSTRAINT PK_Transactions PRIMARY KEY (Id),
        CONSTRAINT CK_Transactions_Type CHECK ([Type] IN (1, 2)),
        CONSTRAINT CK_Transactions_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_Transactions_UnitPrice CHECK (UnitPrice > 0)
    );

    -- Historial por producto filtrado por fechas
    CREATE INDEX IX_Transactions_ProductId_Date ON dbo.Transactions (ProductId, [Date]);
    -- Listado general filtrado por fechas y tipo
    CREATE INDEX IX_Transactions_Date_Type ON dbo.Transactions ([Date], [Type]);
END
GO

PRINT N'Bases de datos de Inventory creadas correctamente.';
GO
