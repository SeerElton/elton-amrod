using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderManagement.Api.Controllers;
using OrderManagement.Application.Services;
using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.ApiTests;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILogger<OrdersController>> _mockLogger;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLogger = new Mock<ILogger<OrdersController>>();
        _controller = new OrdersController(_mockOrderService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "USD",
            TotalAmount = 100m,
            LineItems = new List<CreateOrderLineItemRequest>()
        };

        var response = new OrderResponse
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = "Pending",
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItemResponse>()
        };

        _mockOrderService.Setup(s => s.CreateOrderAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateOrder(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(OrdersController.GetOrder), createdResult.ActionName);
        Assert.Equal(response.Id, ((OrderResponse)createdResult.Value!).Id);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidRequest_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "INVALID",
            TotalAmount = 100m,
            LineItems = new List<CreateOrderLineItemRequest>()
        };

        _mockOrderService.Setup(s => s.CreateOrderAsync(request))
            .ThrowsAsync(new ArgumentException("Invalid currency code"));

        // Act
        var result = await _controller.CreateOrder(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(badRequest.Value);
        Assert.NotNull(errorResponse.Message);
    }

    [Fact]
    public async Task GetOrder_WithValidId_Returns200Ok()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var response = new OrderResponse
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = "Pending",
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItemResponse>()
        };

        _mockOrderService.Setup(s => s.GetOrderAsync(orderId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.Equal(orderId, returnedResponse.Id);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderService.Setup(s => s.GetOrderAsync(orderId))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(notFoundResult.Value);
        Assert.NotNull(errorResponse.Message);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidTransition_Returns200Ok()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest { Status = "Paid" };

        var response = new OrderResponse
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = "Paid",
            CurrencyCode = "USD",
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<OrderLineItemResponse>()
        };

        _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(orderId, request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateOrderStatus(orderId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.Equal("Paid", returnedResponse.Status);
    }

    [Fact]
    public async Task GetAllOrders_Returns200Ok()
    {
        // Act
        var result = await _controller.GetAllOrders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var orders = Assert.IsAssignableFrom<IEnumerable<OrderResponse>>(okResult.Value);
    }

    [Fact]
    public async Task GetCustomerOrders_WithValidCustomerId_Returns200Ok()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = new List<OrderResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Status = "Pending",
                CurrencyCode = "USD",
                TotalAmount = 100m,
                CreatedAt = DateTime.UtcNow,
                LineItems = new List<OrderLineItemResponse>()
            }
        };

        _mockOrderService.Setup(s => s.GetCustomerOrdersAsync(customerId))
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.GetCustomerOrders(customerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedOrders = Assert.IsType<List<OrderResponse>>(okResult.Value);
        Assert.Single(returnedOrders);
        Assert.Equal(customerId, returnedOrders[0].CustomerId);
    }
}
