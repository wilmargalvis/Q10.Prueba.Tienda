IF OBJECT_ID(N'[Inventory].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Inventory') IS NULL EXEC(N'CREATE SCHEMA [Inventory];');
    CREATE TABLE [Inventory].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Inventory].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialInventory'
)
BEGIN
    IF SCHEMA_ID(N'Inventory') IS NULL EXEC(N'CREATE SCHEMA [Inventory];');
END;

IF NOT EXISTS (
    SELECT * FROM [Inventory].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialInventory'
)
BEGIN
    CREATE TABLE [Inventory].[Stock] (
        [Sku] nvarchar(64) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Available] int NOT NULL,
        CONSTRAINT [PK_Stock] PRIMARY KEY ([Sku]),
        CONSTRAINT [CK_Stock_Available] CHECK ([Available] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Inventory].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialInventory'
)
BEGIN
    CREATE TABLE [Inventory].[ProcessedEvents] (
        [EventId] uniqueidentifier NOT NULL,
        [ProcessedAt] datetimeoffset NOT NULL,
        [WasReserved] bit NOT NULL,
        CONSTRAINT [PK_ProcessedEvents] PRIMARY KEY ([EventId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Inventory].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611160001_InitialInventory'
)
BEGIN
    INSERT INTO [Inventory].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'202611160001_InitialInventory', N'10.0.11');
END;

COMMIT;
GO

