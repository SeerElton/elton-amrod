using NUnit.Framework;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Repositories;
using OrderManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderManagement.Infrastructure.Tests;

public class OutboxRepositoryTests
{
    private OrderManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrderManagementDbContext(options);
    }

    [Test]
    public async Task AddAsync_PersistsMessage()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OutboxRepository(dbContext);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreatedEvent",
            Payload = JsonSerializer.Serialize(new { OrderId = Guid.NewGuid(), Amount = 100 }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(message);

        // Assert
        var persisted = dbContext.OutboxMessages.FirstOrDefault(m => m.Id == message.Id);
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted.EventType, Is.EqualTo(message.EventType));
        Assert.That(persisted.Processed, Is.False);
    }

    [Test]
    public async Task GetUnprocessedAsync_ReturnsOnlyUnprocessed()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OutboxRepository(dbContext);

        var unprocessedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreatedEvent",
            Payload = "{}",
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        var processedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderShippedEvent",
            Payload = "{}",
            Processed = true,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.OutboxMessages.AddRange(unprocessedMessage, processedMessage);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetUnprocessedAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        var messages = result.ToList();
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Id, Is.EqualTo(unprocessedMessage.Id));
    }

    [Test]
    public async Task MarkAsProcessedAsync_UpdatesProcessedFlag()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OutboxRepository(dbContext);
        var messageId = Guid.NewGuid();

        var message = new OutboxMessage
        {
            Id = messageId,
            EventType = "OrderCreatedEvent",
            Payload = "{}",
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        // Act
        await repository.MarkAsProcessedAsync(messageId);

        // Assert
        var updated = dbContext.OutboxMessages.Find(messageId);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated.Processed, Is.True);
        Assert.That(updated.ProcessedAt, Is.Not.Null);
    }

    [Test]
    public async Task GetUnprocessedAsync_ReturnsEmpty_WhenNoUnprocessedMessages()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OutboxRepository(dbContext);

        // Act
        var result = await repository.GetUnprocessedAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var repository = new OutboxRepository(dbContext);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderStatusChangedEvent",
            Payload = JsonSerializer.Serialize(new 
            { 
                OrderId = Guid.NewGuid(), 
                Status = "Fulfilled"
            }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(message);
        await repository.SaveChangesAsync();

        // Assert
        var saved = dbContext.OutboxMessages.FirstOrDefault(m => m.Id == message.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.Payload, Does.Contain("Fulfilled"));
    }
}
