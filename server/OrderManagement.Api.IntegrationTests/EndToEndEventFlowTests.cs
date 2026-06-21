using NUnit.Framework;
using OrderManagement.Api;
using OrderManagement.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderManagement.Api.IntegrationTests;

public class EndToEndEventFlowTests
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

    private async Task<string> CreateCustomerAndGetId(HttpClient client, string name)
    {
        var customerRequest = new { 
            id = Guid.NewGuid(), 
            name = name,
            email = $"{name.ToLower()}@test.com",
            phone = "+1234567890",
            address = "123 Test St",
            countryCode = "US"
        };

        var customerResponse = await client.PostAsJsonAsync("/api/customers", customerRequest);
        var customerJson = await customerResponse.Content.ReadAsStringAsync();
        var customerData = JsonSerializer.Deserialize<JsonElement>(customerJson);
        return customerData.GetProperty("id").GetString() ?? Guid.Empty.ToString();
    }

    [Test]
    public async Task CreateOrder_CreatesOrderAndOutboxEvent()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client, "Event Test Customer");

        var orderRequest = new {
            customerId = customerId,
            totalAmount = 150.00m,
            currencyCode = "USD",
            lineItems = new[] {
                new {
                    productSku = "TEST-SKU-001",
                    quantity = 3,
                    unitPrice = 50.00m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", orderRequest);
        var orderJson = await response.Content.ReadAsStringAsync();
        var order = JsonSerializer.Deserialize<JsonElement>(orderJson);
        var orderId = order.GetProperty("id").GetString();

        // Get the dbContext to verify outbox message
        using var scope = _factory.Services.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await Task.Delay(200);

        var unprocessedMessages = await outboxRepository.GetUnprocessedAsync();
        var messageList = new List<OrderManagement.Domain.Entities.OutboxMessage>(unprocessedMessages);

        // Assert
        Assert.That(orderId, Is.Not.Null);
        Assert.That(messageList.Count, Is.GreaterThan(0), "Outbox should contain at least one unprocessed event");
        
        var orderCreatedEvent = messageList.Find(m => m.EventType.Contains("OrderCreatedEvent"));
        Assert.That(orderCreatedEvent, Is.Not.Null);
        Assert.That(orderCreatedEvent.Processed, Is.False);
        Assert.That(orderCreatedEvent.Payload, Does.Contain(orderId));
        Assert.That(orderCreatedEvent.Payload, Does.Contain("CustomerId"));
        Assert.That(orderCreatedEvent.Payload, Does.Contain("TotalAmount"));
    }

    [Test]
    public async Task OrderStatusUpdate_CreatesStatusChangeEvent()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client, "Status Test Customer");

        var orderRequest = new {
            customerId = customerId,
            totalAmount = 200.00m,
            currencyCode = "EUR",
            lineItems = new[] {
                new {
                    productSku = "STATUS-TEST-001",
                    quantity = 2,
                    unitPrice = 100.00m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", orderRequest);
        var orderContent = await createResponse.Content.ReadAsStringAsync();
        var order = JsonSerializer.Deserialize<JsonElement>(orderContent);
        var orderId = order.GetProperty("id").GetString();

        // Act - Update order status
        var updateRequest = new {
            status = "Fulfilled",
            reason = "Order fulfilled"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status", updateRequest);

        // Get outbox messages
        using var scope = _factory.Services.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await Task.Delay(200);

        var unprocessedMessages = await outboxRepository.GetUnprocessedAsync();
        var messageList = new List<OrderManagement.Domain.Entities.OutboxMessage>(unprocessedMessages);

        // Assert
        Assert.That(updateResponse.IsSuccessStatusCode, Is.True);
        Assert.That(messageList.Count, Is.GreaterThan(0), "Outbox should contain status change event");
        
        var statusChangeEvent = messageList.Find(m => m.EventType.Contains("OrderStatusChangedEvent"));
        Assert.That(statusChangeEvent, Is.Not.Null);
        Assert.That(statusChangeEvent.Payload, Does.Contain("Fulfilled"));
        Assert.That(statusChangeEvent.Payload, Does.Contain("CustomerId"));
    }

    [Test]
    public async Task MultipleOrders_CreateMultipleOutboxEvents()
    {
        // Arrange
        var customerId = await CreateCustomerAndGetId(_client, "Bulk Test Customer");

        // Act - Create multiple orders
        var orderIds = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var orderRequest = new {
                customerId = customerId,
                totalAmount = 50.00m * (i + 1),
                currencyCode = "USD",
                lineItems = new[] {
                    new {
                        productSku = $"BULK-SKU-{i:D3}",
                        quantity = i + 1,
                        unitPrice = 50.00m
                    }
                }
            };

            var response = await _client.PostAsJsonAsync("/api/orders", orderRequest);
            var orderJson = await response.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<JsonElement>(orderJson);
            orderIds.Add(order.GetProperty("id").GetString());
        }

        // Get outbox messages
        using var scope = _factory.Services.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await Task.Delay(200);

        var unprocessedMessages = await outboxRepository.GetUnprocessedAsync();
        var messageList = new List<OrderManagement.Domain.Entities.OutboxMessage>(unprocessedMessages);

        // Assert
        Assert.That(orderIds.Count, Is.EqualTo(3));
        Assert.That(messageList.Count, Is.GreaterThanOrEqualTo(3), $"Outbox should contain at least 3 events, found {messageList.Count}");
    }
}
