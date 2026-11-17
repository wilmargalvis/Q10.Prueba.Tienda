using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Inventory.Dominio.Stock;

namespace Q10.Prueba.Inventory.Infraestructura;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> Stock => Set<StockItem>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Inventory");
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("Stock", table => table.HasCheckConstraint("CK_Stock_Available", "[Available] >= 0"));
            entity.HasKey(x => x.Sku);
            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        });
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");
            entity.HasKey(x => x.EventId);
        });
    }
}
