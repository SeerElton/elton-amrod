using NUnit.Framework;
using OrderManagement.Api;
using OrderManagement.Contracts.Requests;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderManagement.Api.IntegrationTests;

public class OrdersControllerIntegrationTests
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    private async Task<string> CreateCustomerAndGetId(HttpClient client)
    {
        var customerRequest = new
        {
            id = Guid.NewGuid(),
            name = "Test Customer",
            email = "test@example.com",
            phone = "+1234567890",
            address = "123 Main St",
            countryCode = "US"
        };

        var customerResponse = await client.PostAsJsonAsync("/api/customers", customerRequest);
        var customerJson = await customerResponse.Content.ReadAsStringAsync();
        var customerData = JsonSerializer.Deserialize<JsonElement>(customerJson);
        return customerData.GetProperty("id").GetString() ?? Guid.Empty.ToString();
    }

    [Test]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedStatusAndOrder()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client);

        var orderRequest = new
        {
            customerId = customerId,
            totalAmount = 99.99m,
            currencyCode = "USD",
            lineItems = new[] {
                new {
                    productSku = "PROD001",
                    quantity = 2,
                    unitPrice = 49.995m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var content = await response.Content.ReadAsStringAsync();
        var order = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(order, Is.Not.Null);
        Assert.That(order.GetProperty("id").GetString(), Is.Not.EqualTo(Guid.Empty.ToString()));
        Assert.That(order.GetProperty("customerId").GetString(), Is.EqualTo(customerId));
        Assert.That(order.GetProperty("totalAmount").GetDouble(), Is.GreaterThan(0));
        Assert.That(order.GetProperty("currencyCode").GetString(), Is.EqualTo("USD"));
    }

    [Test]
    public async Task GetOrder_WithValidId_ReturnsOrder()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client);

        var orderRequest = new
        {
            customerId = customerId,
            totalAmount = 99.99m,
            currencyCode = "USD",
            lineItems = new[] {
                new {
                    productSku = "PROD001",
                    quantity = 2,
                    unitPrice = 49.995m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", orderRequest);
        var orderContent = await createResponse.Content.ReadAsStringAsync();
        var order = JsonSerializer.Deserialize<JsonElement>(orderContent);
        var orderId = order.GetProperty("id").GetString();

        // Act
        var response = await _client.GetAsync($"/api/orders/{orderId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var retrieved = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.That(retrieved.GetProperty("id").GetString(), Is.EqualTo(orderId));
    }

    [Test]
    public async Task GetOrder_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{invalidId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetAllOrders_ReturnsListOfOrders()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CreateOrder_WithMissingCustomerId_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new
        {
            totalAmount = 99.99m,
            currencyCode = "USD",
            lineItems = new[] {
                new {
                    productSku = "PROD001",
                    quantity = 2,
                    unitPrice = 49.995m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", invalidRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateOrder_WithInvalidCurrency_ReturnsBadRequest()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client);

        var invalidRequest = new
        {
            customerId = customerId,
            totalAmount = 99.99m,
            currencyCode = "INVALID",
            lineItems = new[] {
                new {
                    productSku = "PROD001",
                    quantity = 2,
                    unitPrice = 49.995m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", invalidRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdateOrderStatus_WithValidRequest_ChangesStatus()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client);

        var orderRequest = new
        {
            customerId = customerId,
            totalAmount = 99.99m,
            currencyCode = "USD",
            lineItems = new[] {
                new {
                    productSku = "PROD001",
                    quantity = 1,
                    unitPrice = 99.99m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", orderRequest);
        var orderContent = await createResponse.Content.ReadAsStringAsync();
        var order = JsonSerializer.Deserialize<JsonElement>(orderContent);
        var orderId = order.GetProperty("id").GetString();

        var statusUpdateRequest = new
        {
            status = "Fulfilled",
            reason = "Order fulfilled"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status", statusUpdateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.That(updated.GetProperty("status").GetString(), Is.EqualTo("Fulfilled"));
    }
}
