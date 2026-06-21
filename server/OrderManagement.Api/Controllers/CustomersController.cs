using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Services;
using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet("search")]
    [SwaggerOperation(Summary = "Search customers", Description = "Search customers by email or name (partial match)")]
    [ProducesResponseType(typeof(IEnumerable<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchCustomers([FromQuery] string query)
    {
        try
        {
            var results = await _customerService.SearchCustomersAsync(query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomersController->SearchCustomers failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get a customer by ID", Description = "Returns a single customer by their unique identifier")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        try
        {
            var customer = await _customerService.GetCustomerAsync(id);
            if (customer == null)
                return NotFound(new ApiErrorResponse("Customer not found"));

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomersController->GetCustomer failed for {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new customer", Description = "Creates a new customer. Email must be unique.")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var result = await _customerService.CreateCustomerAsync(request);
            _logger.LogInformation("Customer created: {CustomerId} ({Email})", result.Id, result.Email);
            return CreatedAtAction(nameof(GetCustomer), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Customer creation conflict: {Message}", ex.Message);
            return Conflict(new ApiErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Customer validation failed: {Message}", ex.Message);
            return BadRequest(new ApiErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomersController->CreateCustomer failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("An unexpected error occurred"));
        }
    }
}