using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Worker.Consumers;
using Serilog;

var builder = Host.CreateDefaultBuilder(args);

// Serilog Configuration
builder.UseSerilog((context, configuration) =>
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console()
);

builder.ConfigureServices((context, services) =>
{
    // Database
    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost,1433;Database=OrderManagement;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;";

    services.AddDbContext<OrderManagementDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    // RabbitMQ Configuration
    var rabbitMqSettings = context.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
        ?? new RabbitMqSettings();
    services.AddSingleton(rabbitMqSettings);
    services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();

    // Background Services
    services.AddHostedService<OutboxPublisher>();
    services.AddHostedService<OrderCreatedEventConsumer>();
    services.AddHostedService<OrderStatusChangedEventConsumer>();
});

var host = builder.Build();
await host.RunAsync();
