using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Q10.Prueba.Inventory.Dominio.Stock;

namespace Q10.Prueba.Inventory.Infraestructura;

public sealed class InventoryService(InventoryDbContext db, ILogger<InventoryService> logger) : IInventoryService
{
    public async Task<ReservationResult> ReserveAsync(Guid eventId, string sku, int quantity, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var processed = await db.ProcessedEvents.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (processed is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return processed.WasReserved ? ReservationResult.DuplicateReserved : ReservationResult.DuplicateRejected;
        }

        var stock = await db.Stock.SingleOrDefaultAsync(x => x.Sku == sku, cancellationToken);
        var reserved = stock is not null && quantity <= 100 && stock.TryReserve(quantity);
        db.ProcessedEvents.Add(ProcessedEvent.Create(eventId, reserved));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return reserved ? ReservationResult.Reserved : ReservationResult.Rejected;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(exception, "Conflicto al persistir el evento {EventId}; MassTransit podrá reintentarlo.", eventId);
            throw new InvalidOperationException("No se pudo guardar la reserva de inventario.", exception);
        }
    }
}
