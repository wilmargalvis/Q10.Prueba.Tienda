using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Q10.Prueba.Inventory.Infraestructura;
using Q10.Prueba.Tienda.Infraestructura;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Store")
    ?? throw new InvalidOperationException("Falta ConnectionStrings__Store.");
var bootstrapPath = Environment.GetEnvironmentVariable("DATABASE_BOOTSTRAP") ?? "/app/Database/01_CreateDatabase.sql";
var schemaBootstrapPath = Environment.GetEnvironmentVariable("SCHEMA_BOOTSTRAP") ?? "/app/Database/02_CreateSchemas.sql";
var outputDirectory = Environment.GetEnvironmentVariable("MIGRATION_OUTPUT") ?? "/generated";

var builder = new SqlConnectionStringBuilder(connectionString);
var databaseName = builder.InitialCatalog;
if (string.IsNullOrWhiteSpace(databaseName)) throw new InvalidOperationException("La cadena de conexión debe indicar Initial Catalog.");

var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
await using (var master = new SqlConnection(masterBuilder.ConnectionString))
{
    await master.OpenAsync();
    var bootstrap = await File.ReadAllTextAsync(bootstrapPath);
    await using var command = new SqlCommand(bootstrap, master);
    await command.ExecuteNonQueryAsync();
}

await using (var database = new SqlConnection(connectionString))
{
    await database.OpenAsync();
    var schemaBootstrap = await File.ReadAllTextAsync(schemaBootstrapPath);
    await using var command = new SqlCommand(schemaBootstrap, database);
    await command.ExecuteNonQueryAsync();
}

Directory.CreateDirectory(outputDirectory);
await ApplyMigrationsAsync(
    new OrdersDbContext(new DbContextOptionsBuilder<OrdersDbContext>()
        .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Orders"))
        .Options), "03_OrdersSchema.sql");
await ApplyMigrationsAsync(
    new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"))
        .Options), "04_InventorySchema.sql");
Console.WriteLine($"Base {databaseName} creada y migraciones aplicadas.");

async Task ApplyMigrationsAsync(DbContext db, string fileName)
{
    var migrator = db.GetService<IMigrator>();
    var script = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
    var outputPath = Path.Combine(outputDirectory, fileName);
    await File.WriteAllTextAsync(outputPath, script);

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    foreach (var batch in Regex.Split(script, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                 .Where(batch => !string.IsNullOrWhiteSpace(batch)))
    {
        await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine($"Migración aplicada: {fileName}");
}
