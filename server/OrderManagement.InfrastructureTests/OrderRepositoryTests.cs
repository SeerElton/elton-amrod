using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.InfrastructureTests;

public class OrderRepositoryTests
{
    private readonly OrderManagementDbContext _dbContext;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrderManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new OrderManagementDbContext(options);
        _repository = new OrderRepository(_dbContext);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "test@example.com",
            CountryCode = "US",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = OrderStatus.Pending,
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            Customer = customer
        };

        _dbContext.Orders.Add(order);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrder()
    {
        // Arrange
        var order = _dbContext.Orders.First();
        var orderId = order.Id;

        // Act
        var result = await _repository.GetByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal(OrderStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_EagerLoadsCustomer()
    {
        // Arrange
        var order = _dbContext.Orders.First();

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result?.Customer);
        Assert.NotEmpty(result.Customer.Name);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOrdersForCustomer()
    {
        // Arrange
        var customerId = _dbContext.Customers.First().Id;

        // Act
        var result = await _repository.GetByCustomerIdAsync(customerId);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, o => Assert.Equal(customerId, o.CustomerId));
    }

    [Fact]
    public async Task GetByCustomerIdAsync_WithNoOrders_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetByCustomerIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_AddsOrderToDatabase()
    {
        // Arrange
        var customer = _dbContext.Customers.First();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = OrderStatus.Paid,
            CurrencyCode = "EUR",
            TotalAmount = 250m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.CreateAsync(order);

        // Assert
        var createdOrder = await _dbContext.Orders.FindAsync(order.Id);
        Assert.NotNull(createdOrder);
        Assert.Equal("EUR", createdOrder.CurrencyCode);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOrderInDatabase()
    {
        // Arrange
        var order = _dbContext.Orders.First();
        order.Status = OrderStatus.Fulfilled;

        // Act
        await _repository.UpdateAsync(order);

        // Assert
        var updatedOrder = await _dbContext.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Fulfilled, updatedOrder?.Status);
    }
}
