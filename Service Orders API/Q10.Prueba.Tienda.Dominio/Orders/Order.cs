namespace Q10.Prueba.Tienda.Dominio.Orders;

public sealed class Order
{
    private Order() { }

    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Order Create(string customerName, string sku, int quantity)
    {
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(customerName));
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        if (quantity is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe estar entre 1 y 100.");

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName.Trim(),
            Sku = sku.Trim().ToUpperInvariant(),
            Quantity = quantity,
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Pending) Status = OrderStatus.Confirmed;
    }

    public void Reject()
    {
        if (Status == OrderStatus.Pending) Status = OrderStatus.Rejected;
    }
}
