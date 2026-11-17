namespace Q10.Prueba.Tienda.Api.Models;

public sealed class ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Result { get; init; }
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public List<ApiError> Errors { get; init; } = [];

    public static ApiResponse<T> Success(T result, int statusCode = StatusCodes.Status200OK, string message = "") => new()
    {
        IsSuccess = true, Result = result, Message = message, StatusCode = statusCode
    };

    public static ApiResponse<T> Failure(string message, int statusCode, IEnumerable<ApiError>? errors = null) => new()
    {
        IsSuccess = false, Message = message, StatusCode = statusCode, Errors = errors?.ToList() ?? []
    };
}

public sealed class ApiError
{
    public string Message { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public string? Field { get; init; }
}

public sealed record OrderDto(Guid Id, string ClienteNombre, string Sku, int Cantidad, string Estado, DateTimeOffset CreadoEn);
public sealed record CreateOrderRequest(string ClienteNombre, string Sku, int Cantidad);
