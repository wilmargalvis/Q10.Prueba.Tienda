using System.Text.Json;
using Q10.Prueba.Tienda.Contracts;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Infraestructura;

public sealed class OutboxOrderEventPublisher(OrdersDbContext db) : IOrderEventPublisher
{
    public Task EnqueueCreatedAsync(Order order, CancellationToken cancellationToken)
    {
        var message = new OrderCreated(Guid.NewGuid(), order.Id, order.Sku, order.Quantity, DateTimeOffset.UtcNow);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = message.EventId,
            MessageType = nameof(OrderCreated),
            Payload = JsonSerializer.Serialize(message),
            CreatedAt = message.OccurredAt
        });
        return Task.CompletedTask;
    }
}
