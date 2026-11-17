using Moq;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Api.Test;

public sealed class OrderUseCasesTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IStockCatalogReader> _catalog = new();
    private readonly Mock<IOrderEventPublisher> _publisher = new();
    private readonly OrderUseCases _sut;

    public OrderUseCasesTests()
    {
        _orders.Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _orders.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(x => x.EnqueueCreatedAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _sut = new OrderUseCases(_orders.Object, _catalog.Object, _publisher.Object);
    }

    [Fact]
    public async Task Create_rejects_empty_customer_name()
    {
        var exception = await Assert.ThrowsAsync<OrderValidationException>(() =>
            _sut.CreateAsync(new CreateOrderCommand(" ", "GTR-ACU-001", 1), CancellationToken.None));

        Assert.Equal("clienteNombre", exception.Field);
        _orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_rejects_quantity_outside_allowed_range()
    {
        var exception = await Assert.ThrowsAsync<OrderValidationException>(() =>
            _sut.CreateAsync(new CreateOrderCommand("Ada", "GTR-ACU-001", 101), CancellationToken.None));

        Assert.Equal("QUANTITY_OUT_OF_RANGE", exception.Code);
        _catalog.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_rejects_unknown_sku_without_saving()
    {
        _catalog.Setup(x => x.ExistsAsync("UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<OrderValidationException>(() =>
            _sut.CreateAsync(new CreateOrderCommand("Ada", "unknown", 1), CancellationToken.None));

        Assert.Equal("SKU_NOT_FOUND", exception.Code);
        _orders.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_saves_pending_order_and_enqueues_event()
    {
        _catalog.Setup(x => x.ExistsAsync("KBD-001", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.CreateAsync(new CreateOrderCommand("Ada", "kbd-001", 2), CancellationToken.None);

        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal("KBD-001", result.Sku);
        _orders.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(x => x.EnqueueCreatedAsync(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Repeated_stock_result_does_not_change_final_order_state()
    {
        var order = Order.Create("Ada", "GTR-ACU-001", 1);
        _orders.Setup(x => x.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await _sut.ApplyReservationAsync(order.Id, reserved: true, CancellationToken.None);
        await _sut.ApplyReservationAsync(order.Id, reserved: false, CancellationToken.None);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        _orders.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
