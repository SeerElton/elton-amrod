using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using Serilog;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AMROD Order Management API",
        Version = "v1",
        Description = "Senior Developer Technical Assessment - Order Management System"
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=OrderManagement;Trusted_Connection=true;TrustServerCertificate=true;";

builder.Services.AddDbContext<OrderManagementDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Services
builder.Services.AddScoped<IOrderService, OrderService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AMROD API v1");
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
