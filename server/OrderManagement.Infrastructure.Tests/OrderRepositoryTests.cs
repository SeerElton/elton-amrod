using NUnit.Framework;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Repositories;
using OrderManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderManagement.Infrastructure.Tests;

public class OrderRepositoryTests
{
    private OrderManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrderManagementDbContext(options);
    }

    [Test]
    public async Task CreateAsync_PersistsOrderToDatabase()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var customerId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductSku = "SKU001",
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        await repository.CreateAsync(order);

        // Assert
        var retrievedOrder = dbContext.Orders.Include(o => o.LineItems)
            .FirstOrDefault(o => o.Id == order.Id);

        Assert.That(retrievedOrder, Is.Not.Null);
        Assert.That(retrievedOrder.CustomerId, Is.EqualTo(order.CustomerId));
        Assert.That(retrievedOrder.TotalAmount, Is.EqualTo(100.00m));
        Assert.That(retrievedOrder.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(retrievedOrder.LineItems, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsOrderWithLineItems()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            TotalAmount = 150.00m,
            CurrencyCode = "EUR",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductSku = "SKU001",
                    Quantity = 1,
                    UnitPrice = 100.00m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductSku = "SKU002",
                    Quantity = 1,
                    UnitPrice = 50.00m
                }
            }
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(orderId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(orderId));
        Assert.That(result.LineItems.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNullWhenOrderNotFound()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);

        var orders = new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Pending,
                TotalAmount = 100.00m,
                CurrencyCode = "USD",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Fulfilled,
                TotalAmount = 200.00m,
                CurrencyCode = "EUR",
                CreatedAt = DateTime.UtcNow
            }
        };

        dbContext.Orders.AddRange(orders);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateAsync_ModifiesOrderStatus()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act
        order.Status = OrderStatus.Fulfilled;
        await repository.UpdateAsync(order);

        // Assert
        var updated = dbContext.Orders.Find(orderId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Status, Is.EqualTo(OrderStatus.Fulfilled));
    }

    [Test]
    public async Task UpdateAsync_UpdatesOrderStatus()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act
        order.Status = OrderStatus.Fulfilled;
        await repository.UpdateAsync(order);

        // Assert
        var updated = await repository.GetByIdAsync(orderId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Status, Is.EqualTo(OrderStatus.Fulfilled));
    }

    [Test]
    public async Task CreateAsync_WithNullLineItems_CreatesOrderSuccessfully()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>()
        };

        // Act
        await repository.CreateAsync(order);

        // Assert
        var retrieved = await repository.GetByIdAsync(order.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.LineItems, Is.Empty);
    }
}
