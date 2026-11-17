using MassTransit;
using Q10.Prueba.Inventory.Dominio.Stock;
using Q10.Prueba.Tienda.Contracts;

namespace Q10.Prueba.Inventory.Worker.Consumers;

public sealed class OrderCreatedConsumer(IInventoryService inventory) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        // Inventory persists the processed event with the stock update so redelivery is safe.
        var message = context.Message;
        var result = await inventory.ReserveAsync(message.EventId, message.Sku, message.Quantity, context.CancellationToken);
        if (result is ReservationResult.Reserved or ReservationResult.DuplicateReserved)
        {
            await context.Publish(new StockReserved(message.EventId, message.OrderId, message.Sku, message.Quantity, DateTimeOffset.UtcNow));
            return;
        }

        await context.Publish(new StockRejected(message.EventId, message.OrderId, message.Sku, message.Quantity,
            "No existe el SKU o no hay unidades suficientes.", DateTimeOffset.UtcNow));
    }
}
