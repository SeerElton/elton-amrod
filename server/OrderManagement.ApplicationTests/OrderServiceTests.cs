using Xunit;
using Moq;
using OrderManagement.Application.Services;
using OrderManagement.Contracts.Requests;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.ApplicationTests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IOutboxRepository> _mockOutboxRepository;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockOutboxRepository = new Mock<IOutboxRepository>();
        _orderService = new OrderService(_mockOrderRepository.Object, _mockOutboxRepository.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_ReturnsOrderResponse()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            CurrencyCode = "USD",
            TotalAmount = 100.00m,
            LineItems = new List<CreateOrderLineItemRequest>
            {
                new() { ProductSku = "WIDGET-001", Quantity = 2, UnitPrice = 50m }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Pending, Enum.Parse<OrderStatus>(result.Status));
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(100.00m, result.TotalAmount);
        Assert.Single(result.LineItems);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInvalidCurrencyCode_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "INVALID",
            TotalAmount = 100m,
            LineItems = new List<CreateOrderLineItemRequest>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_WithNegativeTotalAmount_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "USD",
            TotalAmount = -100m,
            LineItems = new List<CreateOrderLineItemRequest>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(request));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_FromPendingToPaid_Succeeds()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        var request = new UpdateOrderStatusRequest { Status = "Paid" };

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, request);

        // Assert
        Assert.Equal(OrderStatus.Paid, Enum.Parse<OrderStatus>(result.Status));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Fulfilled,
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        var request = new UpdateOrderStatusRequest { Status = "Pending" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _orderService.UpdateOrderStatusAsync(orderId, request));
    }

    [Fact]
    public async Task ValidateStatusTransition_PendingToValid_Returns True()
    {
        // Act
        var result = _orderService.ValidateStatusTransition(OrderStatus.Pending, OrderStatus.Paid);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateStatusTransition_FulfilledToAny_ReturnsFalse()
    {
        // Act
        var result = _orderService.ValidateStatusTransition(OrderStatus.Fulfilled, OrderStatus.Paid);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetOrderAsync_WithValidId_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItem>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetOrderAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _orderService.GetOrderAsync(Guid.NewGuid()));
    }
}
