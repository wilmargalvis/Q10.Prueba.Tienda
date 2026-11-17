using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Q10.Prueba.Inventory.Infraestructura.Migrations;

[DbContext(typeof(InventoryDbContext))]
[Migration("202611160001_InitialInventory")]
public sealed class InitialInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("Inventory");
        migrationBuilder.CreateTable(
            name: "Stock", schema: "Inventory",
            columns: table => new
            {
                Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Available = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stock", x => x.Sku);
                table.CheckConstraint("CK_Stock_Available", "[Available] >= 0");
            });
        migrationBuilder.CreateTable(
            name: "ProcessedEvents", schema: "Inventory",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                WasReserved = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ProcessedEvents", x => x.EventId));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ProcessedEvents", "Inventory");
        migrationBuilder.DropTable("Stock", "Inventory");
    }
}
