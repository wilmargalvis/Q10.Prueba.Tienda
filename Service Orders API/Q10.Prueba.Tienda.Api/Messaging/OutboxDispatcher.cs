using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Tienda.Contracts;
using Q10.Prueba.Tienda.Infraestructura;

namespace Q10.Prueba.Tienda.Api.Messaging;

public sealed class OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = false;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var messages = await db.OutboxMessages.Where(x => x.ProcessedAt == null)
                    .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    var created = JsonSerializer.Deserialize<OrderCreated>(message.Payload);
                    if (created is null) throw new InvalidOperationException($"Outbox message {message.Id} no se pudo deserializar.");
                    await bus.Publish(created, stoppingToken);
                    message.ProcessedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    published = true;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "No se pudo publicar un mensaje del outbox; se reintentará.");
            }

            if (!published) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
