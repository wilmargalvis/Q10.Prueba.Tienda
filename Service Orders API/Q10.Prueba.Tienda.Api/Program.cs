using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Q10.Prueba.Tienda.Api;
using Q10.Prueba.Tienda.Api.Messaging;
using Q10.Prueba.Tienda.Api.Middleware;
using Q10.Prueba.Tienda.Api.Models;
using Q10.Prueba.Tienda.Dominio.Orders;
using Q10.Prueba.Tienda.Infraestructura;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .SelectMany(entry => entry.Value?.Errors.Select(error => new ApiError
            {
                Field = string.IsNullOrEmpty(entry.Key) || entry.Key is "$" or "request"
                    ? null
                    : char.ToLowerInvariant(entry.Key[0]) + entry.Key[1..],
                Message = string.IsNullOrWhiteSpace(error.ErrorMessage) ? "El valor enviado no es válido." : error.ErrorMessage,
                ErrorCode = "REQUEST_INVALID"
            }) ?? []);

        return new BadRequestObjectResult(ApiResponse<object>.Failure(
            "La solicitud contiene errores.", StatusCodes.Status400BadRequest, errors));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddScoped<OrderUseCases>();
builder.Services.AddHostedService<OrdersMigrationHostedService>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedConsumer>();
    x.AddConsumer<StockRejectedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        cfg.UseMessageRetry(retry => retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5)));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

public partial class Program { }
