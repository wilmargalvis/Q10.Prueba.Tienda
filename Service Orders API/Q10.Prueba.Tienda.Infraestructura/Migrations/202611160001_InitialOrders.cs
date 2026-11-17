using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Q10.Prueba.Tienda.Infraestructura.Migrations;

[DbContext(typeof(OrdersDbContext))]
[Migration("202611160001_InitialOrders")]
public sealed class InitialOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("Orders");
        migrationBuilder.CreateTable(
            name: "Orders", schema: "Orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.CheckConstraint("CK_Orders_Quantity", "[Quantity] BETWEEN 1 AND 100");
            });
        migrationBuilder.CreateIndex("IX_Orders_Status_CreatedAt", "Orders", new[] { "Status", "CreatedAt" }, schema: "Orders");
        migrationBuilder.CreateTable(
            name: "OutboxMessages", schema: "Orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            }, constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));
        migrationBuilder.CreateIndex("IX_OutboxMessages_ProcessedAt_CreatedAt", "OutboxMessages", new[] { "ProcessedAt", "CreatedAt" }, schema: "Orders");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OutboxMessages", "Orders");
        migrationBuilder.DropTable("Orders", "Orders");
    }
}
