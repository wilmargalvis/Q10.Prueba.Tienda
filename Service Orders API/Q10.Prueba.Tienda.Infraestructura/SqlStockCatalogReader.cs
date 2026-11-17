using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Infraestructura;

public sealed class SqlStockCatalogReader(IConfiguration configuration) : IStockCatalogReader
{
    private readonly string _connectionString = configuration.GetConnectionString("Store")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:Store.");

    public async Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [Inventory].[Stock] WHERE [Sku] = @sku) THEN 1 ELSE 0 END";
        command.Parameters.AddWithValue("@sku", sku);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }
}
