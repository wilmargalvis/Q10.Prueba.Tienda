using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Infraestructura;

public sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken) => await db.Orders.AddAsync(order, cancellationToken);
    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) => db.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken) =>
        await db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
