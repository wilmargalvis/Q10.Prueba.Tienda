using Q10.Prueba.Tienda.Api.Models;
using Q10.Prueba.Tienda.Dominio.Orders;

namespace Q10.Prueba.Tienda.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return;
            logger.LogError(exception, "Error no controlado procesando {Path}", context.Request.Path);
            var status = exception is OrderValidationException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
            List<ApiError> errors = exception is OrderValidationException validation
                ? [new ApiError { Field = validation.Field, Message = validation.Message, ErrorCode = validation.Code }]
                : [];
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(
                exception is OrderValidationException ? "La solicitud contiene errores." : "Ocurrió un error procesando la solicitud.", status, errors));
        }
    }
}
