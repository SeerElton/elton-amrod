using System.Text.Json;
using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.Application.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse> GetOrderAsync(Guid orderId);
    Task<IEnumerable<OrderResponse>> GetCustomerOrdersAsync(Guid customerId);
    Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxRepository _outboxRepository;

    public OrderService(IOrderRepository orderRepository, IOutboxRepository outboxRepository)
    {
        _orderRepository = orderRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        ValidateOrderRequest(request);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            CurrencyCode = request.CurrencyCode,
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow,
            LineItems = request.LineItems?.Select(li => new OrderLineItem
            {
                Id = Guid.NewGuid(),
                ProductSku = li.ProductSku,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList() ?? new List<OrderLineItem>()
        };

        var createdOrder = await _orderRepository.CreateAsync(order);

        // Create outbox event
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreated",
            Payload = JsonSerializer.Serialize(new { OrderId = createdOrder.Id, CustomerId = createdOrder.CustomerId }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _outboxRepository.AddAsync(outboxMessage);

        return MapToResponse(createdOrder);
    }

    public async Task<OrderResponse> GetOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new InvalidOperationException($"Order {orderId} not found");

        return MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetCustomerOrdersAsync(Guid customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
            throw new ArgumentException($"Invalid status: {request.Status}");

        ValidateStatusTransition(order.Status, newStatus);

        order.Status = newStatus;
        await _orderRepository.UpdateAsync(order);

        // Create outbox event
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderStatusUpdated",
            Payload = JsonSerializer.Serialize(new { OrderId = order.Id, Status = newStatus.ToString() }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _outboxRepository.AddAsync(outboxMessage);

        return MapToResponse(order);
    }

    private void ValidateOrderRequest(CreateOrderRequest request)
    {
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required");

        if (string.IsNullOrWhiteSpace(request.CurrencyCode) || request.CurrencyCode.Length != 3)
            throw new ArgumentException("Currency code must be 3 characters");

        if (request.TotalAmount <= 0)
            throw new ArgumentException("Total amount must be greater than 0");
    }

    private void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        var allowedTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Pending, new[] { OrderStatus.Paid, OrderStatus.Cancelled } },
            { OrderStatus.Paid, new[] { OrderStatus.Fulfilled, OrderStatus.Cancelled } },
            { OrderStatus.Fulfilled, Array.Empty<OrderStatus>() },
            { OrderStatus.Cancelled, Array.Empty<OrderStatus>() }
        };

        if (!allowedTransitions.ContainsKey(currentStatus) || !allowedTransitions[currentStatus].Contains(newStatus))
            throw new InvalidOperationException($"Cannot transition from {currentStatus} to {newStatus}");
    }

    private OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            CurrencyCode = order.CurrencyCode,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            LineItems = order.LineItems?.Select(li => new OrderLineItemResponse
            {
                Id = li.Id,
                ProductSku = li.ProductSku,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList()
        };
    }
}
