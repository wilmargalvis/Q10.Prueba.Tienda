namespace Q10.Prueba.Tienda.Contracts;

public sealed record OrderCreated(Guid EventId, Guid OrderId, string Sku, int Quantity, DateTimeOffset OccurredAt);
public sealed record StockReserved(Guid EventId, Guid OrderId, string Sku, int Quantity, DateTimeOffset OccurredAt);
public sealed record StockRejected(Guid EventId, Guid OrderId, string Sku, int Quantity, string Reason, DateTimeOffset OccurredAt);

