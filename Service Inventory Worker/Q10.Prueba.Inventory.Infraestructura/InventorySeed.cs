using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Inventory.Dominio.Stock;

namespace Q10.Prueba.Inventory.Infraestructura;

public static class InventorySeed
{
    private static readonly (string Sku, string Name, int Available)[] Products =
    [
        ("GTR-ACU-001", "Guitarra acústica", 12),
        ("KBD-001", "Teclado musical", 8),
        ("DRM-001", "Batería acústica", 3)
    ];

    public static async Task SeedAsync(InventoryDbContext db, CancellationToken cancellationToken)
    {
        foreach (var product in Products)
        {
            if (!await db.Stock.AnyAsync(x => x.Sku == product.Sku, cancellationToken))
                db.Stock.Add(StockItem.Create(product.Sku, product.Name, product.Available));
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
