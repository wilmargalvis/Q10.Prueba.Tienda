using MassTransit;
using Q10.Prueba.Tienda.Contracts;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Api.Messaging;

public sealed class StockReservedConsumer(OrderUseCases orders) : IConsumer<StockReserved>
{
    public Task Consume(ConsumeContext<StockReserved> context) =>
        orders.ApplyReservationAsync(context.Message.OrderId, true, context.CancellationToken);
}

public sealed class StockRejectedConsumer(OrderUseCases orders) : IConsumer<StockRejected>
{
    public Task Consume(ConsumeContext<StockRejected> context) =>
        orders.ApplyReservationAsync(context.Message.OrderId, false, context.CancellationToken);
}
