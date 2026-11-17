using Microsoft.AspNetCore.Mvc;
using Q10.Prueba.Tienda.Api.Models;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderUseCases useCases) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await useCases.CreateAsync(new CreateOrderCommand(request.ClienteNombre, request.Sku, request.Cantidad), cancellationToken);
        var result = ToDto(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            ApiResponse<OrderDto>.Success(result, StatusCodes.Status201Created, "Pedido recibido para validación de inventario."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> List(CancellationToken cancellationToken)
    {
        var orders = await useCases.ListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderDto>>.Success(orders.Select(ToDto).ToList(), message: "Pedidos consultados."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await useCases.GetAsync(id, cancellationToken);
        if (order is null)
            return NotFound(ApiResponse<object>.Failure("No se encontró el pedido.", StatusCodes.Status404NotFound,
                [new ApiError { Message = "El identificador no corresponde a un pedido.", ErrorCode = "ORDER_NOT_FOUND" }]));
        return Ok(ApiResponse<OrderDto>.Success(ToDto(order), message: "Pedido consultado."));
    }

    private static OrderDto ToDto(Order order) => new(order.Id, order.CustomerName, order.Sku, order.Quantity, order.Status.ToString(), order.CreatedAt);
}
