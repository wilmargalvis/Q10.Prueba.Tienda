using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Q10.Prueba.Inventory.Dominio.Stock;

namespace Q10.Prueba.Inventory.Infraestructura;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Store")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Store.");
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory")));
        services.AddScoped<IInventoryService, InventoryService>();
        return services;
    }
}
