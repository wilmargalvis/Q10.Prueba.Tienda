namespace Q10.Prueba.Tienda.Dominio.Orders;

public sealed class OrderUseCases(
    IOrderRepository orders,
    IStockCatalogReader catalog,
    IOrderEventPublisher publisher)
{
    public async Task<Order> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.CustomerName))
            throw new OrderValidationException("El nombre del cliente es obligatorio.", "clienteNombre", "CUSTOMER_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(command.Sku))
            throw new OrderValidationException("El SKU es obligatorio.", "sku", "SKU_REQUIRED");
        if (command.Quantity is < 1 or > 100)
            throw new OrderValidationException("La cantidad debe estar entre 1 y 100.", "cantidad", "QUANTITY_OUT_OF_RANGE");

        var normalizedSku = command.Sku.Trim().ToUpperInvariant();
        if (!await catalog.ExistsAsync(normalizedSku, cancellationToken))
            throw new OrderValidationException("El SKU no existe en el catálogo.", "sku", "SKU_NOT_FOUND");

        var order = Order.Create(command.CustomerName, normalizedSku, command.Quantity);
        await orders.AddAsync(order, cancellationToken);
        await publisher.EnqueueCreatedAsync(order, cancellationToken);
        // The publisher writes an outbox row through the same scoped DbContext;
        // persisting both together prevents an order from being saved without its event.
        await orders.SaveChangesAsync(cancellationToken);
        return order;
    }

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) => orders.GetAsync(id, cancellationToken);
    public Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken) => orders.ListAsync(cancellationToken);

    public async Task ApplyReservationAsync(Guid orderId, bool reserved, CancellationToken cancellationToken)
    {
        var order = await orders.GetAsync(orderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Pending) return;
        if (reserved) order.Confirm(); else order.Reject();
        await orders.SaveChangesAsync(cancellationToken);
    }
}
