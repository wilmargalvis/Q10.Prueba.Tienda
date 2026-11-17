namespace Q10.Prueba.Inventory.Dominio.Stock;

public sealed class StockItem
{
    private StockItem() { }

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Available { get; private set; }

    public static StockItem Create(string sku, string name, int available) => new()
    {
        Sku = sku.Trim().ToUpperInvariant(),
        Name = name.Trim(),
        Available = Math.Max(0, available)
    };

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0 || Available < quantity) return false;
        Available -= quantity;
        return true;
    }
}

public sealed class ProcessedEvent
{
    private ProcessedEvent() { }
    public Guid EventId { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }
    public bool WasReserved { get; private set; }

    public static ProcessedEvent Create(Guid eventId, bool wasReserved) => new()
    {
        EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow, WasReserved = wasReserved
    };
}

public enum ReservationResult { Reserved, Rejected, DuplicateReserved, DuplicateRejected }

public interface IInventoryService
{
    Task<ReservationResult> ReserveAsync(Guid eventId, string sku, int quantity, CancellationToken cancellationToken);
}
