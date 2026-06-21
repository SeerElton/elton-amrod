using NUnit.Framework;
using Moq;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Contracts.Requests;
using OrderManagement.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderManagement.Application.Tests;

public class OrderServiceTests
{
    private Mock<IOrderRepository> _mockOrderRepository;
    private Mock<IOutboxRepository> _mockOutboxRepository;
    private OrderService _orderService;

    [SetUp]
    public void Setup()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockOutboxRepository = new Mock<IOutboxRepository>();
        _orderService = new OrderService(_mockOrderRepository.Object, _mockOutboxRepository.Object);
    }

    [Test]
    public async Task CreateOrderAsync_WithValidRequest_CreatesOrderAndEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            LineItems = new List<OrderLineItemRequest>
            {
                new() { ProductSku = "SKU001", Quantity = 2, UnitPrice = 50.00m }
            }
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>
            {
                new() { Id = Guid.NewGuid(), ProductSku = "SKU001", Quantity = 2, UnitPrice = 50.00m }
            }
        };

        _mockOrderRepository
            .Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        _mockOutboxRepository
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>()))
            .Returns(Task.CompletedTask);

        _mockOutboxRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CustomerId, Is.EqualTo(customerId));
        Assert.That(result.TotalAmount, Is.EqualTo(100.00m));
        Assert.That(result.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(result.Status, Is.EqualTo("Pending"));

        _mockOrderRepository.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Once);
        _mockOutboxRepository.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>()), Times.Once);
    }

    [Test]
    public async Task CreateOrderAsync_StoresOrderCreatedEventInOutbox()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            LineItems = new List<OrderLineItemRequest>
            {
                new() { ProductSku = "SKU001", Quantity = 2, UnitPrice = 50.00m }
            }
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>
            {
                new() { Id = Guid.NewGuid(), ProductSku = "SKU001", Quantity = 2, UnitPrice = 50.00m }
            }
        };

        OutboxMessage? capturedMessage = null;

        _mockOrderRepository
            .Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);

        _mockOutboxRepository
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>()))
            .Callback<OutboxMessage>(m => capturedMessage = m)
            .Returns(Task.CompletedTask);

        _mockOutboxRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage.EventType, Does.Contain("OrderCreatedEvent"));
        Assert.That(capturedMessage.Processed, Is.False);
        Assert.That(capturedMessage.Payload, Does.Contain("CustomerId"));
        Assert.That(capturedMessage.Payload, Does.Contain("TotalAmount"));
    }

    [Test]
    public async Task GetOrderAsync_ReturnsOrderWhenExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>()
        };

        _mockOrderRepository
            .Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetOrderAsync(orderId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(orderId));
        _mockOrderRepository.Verify(x => x.GetByIdAsync(orderId), Times.Once);
    }

    [Test]
    public async Task GetAllOrdersAsync_ReturnsAllOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                TotalAmount = 100.00m,
                CurrencyCode = "USD",
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                LineItems = new List<OrderLineItem>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                TotalAmount = 200.00m,
                CurrencyCode = "EUR",
                Status = OrderStatus.Fulfilled,
                CreatedAt = DateTime.UtcNow,
                LineItems = new List<OrderLineItem>()
            }
        };

        _mockOrderRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetAllOrdersAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ChangesStatusAndCreatesEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var existingOrder = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>()
        };

        var request = new UpdateOrderStatusRequest
        {
            Status = "Fulfilled",
            Reason = "Order fulfilled"
        };

        _mockOrderRepository
            .Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);

        _mockOrderRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        _mockOutboxRepository
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>()))
            .Returns(Task.CompletedTask);

        _mockOutboxRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Fulfilled"));
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOutboxRepository.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>()), Times.Once);
    }
}
