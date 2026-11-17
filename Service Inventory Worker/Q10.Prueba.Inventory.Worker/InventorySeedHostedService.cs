using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Inventory.Infraestructura;

namespace Q10.Prueba.Inventory.Worker;

public sealed class InventorySeedHostedService(IServiceProvider services, ILogger<InventorySeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await InventorySeed.SeedAsync(db, cancellationToken);
        logger.LogInformation("Catálogo inicial disponible.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
