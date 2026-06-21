using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace OrderManagement.Infrastructure.Messaging;

/// <summary>
/// Background service that publishes outbox messages to RabbitMQ.
/// Polls the Outbox table every 5 seconds for unprocessed events.
/// Implements the Outbox Pattern for reliable message delivery.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<OutboxPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private const int PollIntervalSeconds = 5;
    private const string ExchangeName = "orderflow.events";

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqSettings settings,
        ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _connectionFactory = connectionFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher starting");

        // Initialize RabbitMQ connection
        try
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange for events
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            _logger.LogInformation("RabbitMQ exchange configured: {ExchangeName}", ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            throw;
        }

        // Main processing loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishUnprocessedMessagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxPublisher loop");
                // Continue processing despite errors
            }
        }

        _logger.LogInformation("OutboxPublisher stopping");
    }

    private async Task PublishUnprocessedMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

        var unprocessedMessages = await dbContext.OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        if (unprocessedMessages.Count() == 0)
            return;

        _logger.LogInformation("Publishing {MessageCount} outbox messages", unprocessedMessages.Count());

        foreach (var message in unprocessedMessages)
        {
            try
            {
                await PublishMessageAsync(message, cancellationToken);

                // Mark as processed only after successful publish
                message.Processed = true;
                message.ProcessedAt = DateTime.UtcNow;
                dbContext.OutboxMessages.Update(message);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Published and marked message as processed: {EventType} ({MessageId})",
                    message.EventType, message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message {MessageId}: {EventType}",
                    message.Id, message.EventType);
                // Don't mark as processed; will retry on next iteration
            }
        }
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not initialized");

        var routingKey = GetRoutingKey(message.EventType);
        var body = Encoding.UTF8.GetBytes(message.Payload);

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.Persistent = true;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body
        );

        await Task.CompletedTask;
    }

    private string GetRoutingKey(string eventType) => $"order.{eventType.ToLowerInvariant()}";

    public override void Dispose()
    {
        _logger.LogInformation("OutboxPublisher disposing");

        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
