using MassTransit;
using Q10.Prueba.Inventory.Infraestructura;
using Q10.Prueba.Inventory.Worker;
using Q10.Prueba.Inventory.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddHostedService<InventorySeedHostedService>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
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

await builder.Build().RunAsync();
