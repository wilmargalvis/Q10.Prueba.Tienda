namespace Q10.Prueba.Tienda.Dominio.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IStockCatalogReader
{
    Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken);
}

public interface IOrderEventPublisher
{
    Task EnqueueCreatedAsync(Order order, CancellationToken cancellationToken);
}

public sealed record CreateOrderCommand(string CustomerName, string Sku, int Quantity);

public sealed class OrderValidationException(string message, string field, string code) : Exception(message)
{
    public string Field { get; } = field;
    public string Code { get; } = code;
}
