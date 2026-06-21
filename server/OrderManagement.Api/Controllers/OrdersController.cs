using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Services;
using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new order", Description = "Creates an order for an existing customer with optional line items")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var result = await _orderService.CreateOrderAsync(request);
            _logger.LogInformation("Order created: {OrderId}", result.Id);
            return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new ApiErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrdersController->CreateOrder failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get all orders", Description = "Returns all orders in the system")]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            var result = await _orderService.GetAllOrdersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrdersController->GetAllOrders failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get an order by ID", Description = "Returns a single order with its line items")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        try
        {
            var result = await _orderService.GetOrderAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Order not found: {OrderId}", id);
            return NotFound(new ApiErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrdersController->GetOrder failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }

    [HttpGet("customer/{customerId:guid}")]
    [SwaggerOperation(Summary = "Get orders for a customer", Description = "Returns all orders belonging to a specific customer")]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId)
    {
        try
        {
            var result = await _orderService.GetCustomerOrdersAsync(customerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrdersController->GetCustomerOrders failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }

    [HttpPut("{id:guid}/status")]
    [SwaggerOperation(Summary = "Update order status", Description = "Transitions an order to a new status. Valid transitions: Pending→Paid, Pending→Cancelled, Paid→Fulfilled, Paid→Cancelled")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, request);
            _logger.LogInformation("Order status updated: {OrderId} -> {Status}", id, result.Status);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(new ApiErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new ApiErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrdersController->UpdateOrderStatus failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }
}
