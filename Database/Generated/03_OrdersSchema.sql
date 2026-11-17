IF OBJECT_ID(N'[Orders].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Orders') IS NULL EXEC(N'CREATE SCHEMA [Orders];');
    CREATE TABLE [Orders].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialOrders'
)
BEGIN
    IF SCHEMA_ID(N'Orders') IS NULL EXEC(N'CREATE SCHEMA [Orders];');
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialOrders'
)
BEGIN
    CREATE TABLE [Orders].[Orders] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerName] nvarchar(200) NOT NULL,
        [Sku] nvarchar(64) NOT NULL,
        [Quantity] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Orders_Quantity] CHECK ([Quantity] BETWEEN 1 AND 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_Status_CreatedAt] ON [Orders].[Orders] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialOrders'
)
BEGIN
    CREATE TABLE [Orders].[OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [MessageType] nvarchar(200) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ProcessedAt] datetimeoffset NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_ProcessedAt_CreatedAt] ON [Orders].[OutboxMessages] ([ProcessedAt], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orders].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialOrders'
)
BEGIN
    INSERT INTO [Orders].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'202611160001_InitialOrders', N'10.0.11');
END;

COMMIT;
GO

