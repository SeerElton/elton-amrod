using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Contracts.Events;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderManagement.Worker.Consumers;

/// <summary>
/// Consumes OrderCreatedEvent messages from RabbitMQ.
/// Handles order fulfillment workflows.
/// </summary>
public class OrderCreatedEventConsumer : BackgroundService
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _consumerTag;

    private const string ExchangeName = "amrod.events";
    private const string QueueName = "amrod.order.created";
    private const string RoutingKey = "order.ordercreatedevent";

    public OrderCreatedEventConsumer(
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqSettings settings,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderCreatedEventConsumer starting");

        try
        {
            // Initialize connection
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Configure exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            // Declare queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Bind queue to exchange
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey
            );

            // Set QoS
            _channel.BasicQos(0, _settings.PrefetchCount, false);

            _logger.LogInformation("OrderCreatedEventConsumer: Queue {QueueName} configured and bound to {ExchangeName}",
                QueueName, ExchangeName);

            // Start consuming with async consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += HandleMessageAsync;

            consumer.Shutdown += async (model, ea) =>
            {
                _logger.LogWarning("Consumer shutdown: {ShutdownInitiator}", ea.Initiator);
                await Task.CompletedTask;
            };

            _consumerTag = _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumerTag: "order-created-consumer",
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer
            );

            _logger.LogInformation("Started consuming messages from queue: {QueueName}", QueueName);

            // Keep the consumer running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderCreatedEventConsumer cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in OrderCreatedEventConsumer");
            throw;
        }
    }

    private async Task HandleMessageAsync(object model, BasicDeliverEventArgs ea)
    {
        await HandleMessage(ea);
    }

    private async Task HandleMessage(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize OrderCreatedEvent from message");
                _channel?.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            _logger.LogInformation("Processing OrderCreatedEvent: OrderId={OrderId}, CustomerId={CustomerId}, Amount={Amount}",
                @event.OrderId, @event.CustomerId, @event.TotalAmount);

            // TODO: Implement actual fulfillment logic here
            // - Create fulfillment record
            // - Trigger warehouse notification
            // - Update order status if needed
            // etc.

            await Task.Delay(100); // Simulate processing

            _logger.LogInformation("Completed processing OrderCreatedEvent: OrderId={OrderId}", @event.OrderId);

            // Acknowledge message only after successful processing
            _channel?.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from queue {QueueName}", QueueName);
            // Nack the message to retry later
            _channel?.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("OrderCreatedEventConsumer disposing gracefully");

        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
