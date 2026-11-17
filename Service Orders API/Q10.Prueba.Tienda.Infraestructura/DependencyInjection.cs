using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Infraestructura;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Store")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Store.");
        services.AddDbContext<OrdersDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Orders")));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IStockCatalogReader, SqlStockCatalogReader>();
        services.AddScoped<IOrderEventPublisher, OutboxOrderEventPublisher>();
        return services;
    }
}
