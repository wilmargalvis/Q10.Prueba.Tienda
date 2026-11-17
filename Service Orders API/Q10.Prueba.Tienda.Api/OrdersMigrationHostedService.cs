using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Tienda.Infraestructura;

namespace Q10.Prueba.Tienda.Api;

public sealed class OrdersMigrationHostedService(IServiceProvider services, ILogger<OrdersMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Esquema Orders actualizado.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
