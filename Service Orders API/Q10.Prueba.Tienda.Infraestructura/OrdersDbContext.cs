using Microsoft.EntityFrameworkCore;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Infraestructura;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Orders");
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", table => table.HasCheckConstraint("CK_Orders_Quantity", "[Quantity] BETWEEN 1 AND 100"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MessageType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
        });
    }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
