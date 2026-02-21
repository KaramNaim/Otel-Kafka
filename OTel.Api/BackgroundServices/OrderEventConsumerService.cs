using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using OTel.Domain.Events;
using OTel.Infrastructure.Context;

namespace OTel.Api.BackgroundServices;

public class OrderEventConsumerService : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("OTel.Kafka");
    private static readonly Meter Meter = new("OTel.Kafka");
    private static readonly Counter<long> EventsConsumedCounter = Meter.CreateCounter<long>("orders.events.consumed", "count", "Number of order events consumed from Kafka");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsumerConfig _consumerConfig;
    private readonly ILogger<OrderEventConsumerService> _logger;
    private readonly string _topic;

    public OrderEventConsumerService(
        IServiceScopeFactory scopeFactory,
        ConsumerConfig consumerConfig,
        IConfiguration configuration,
        ILogger<OrderEventConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _consumerConfig = consumerConfig;
        _logger = logger;
        _topic = configuration["Kafka:Topic"] ?? "order-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to let the host finish starting up
        await Task.Yield();

        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka consumer started. Subscribed to topic: {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message is null) continue;

                    using var activity = ActivitySource.StartActivity("Kafka.Consume", ActivityKind.Consumer);
                    activity?.SetTag("messaging.system", "kafka");
                    activity?.SetTag("messaging.destination.name", result.Topic);
                    activity?.SetTag("messaging.kafka.partition", result.Partition.Value);
                    activity?.SetTag("messaging.kafka.offset", result.Offset.Value);
                    activity?.SetTag("messaging.kafka.message.key", result.Message.Key);

                    var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(result.Message.Value);
                    if (orderEvent is null)
                    {
                        _logger.LogWarning("Failed to deserialize message at offset {Offset}", result.Offset.Value);
                        continue;
                    }

                    _logger.LogInformation(
                        "Received OrderCreatedEvent: OrderId={OrderId}, Product={ProductName}, Quantity={Quantity}, Total={TotalPrice}",
                        orderEvent.OrderId, orderEvent.ProductName, orderEvent.Quantity, orderEvent.TotalPrice);

                    activity?.SetTag("order.id", orderEvent.OrderId);

                    // Update order status from Pending to Confirmed
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var order = await dbContext.Orders.FindAsync([orderEvent.OrderId], stoppingToken);
                    if (order is not null && order.Status == "Pending")
                    {
                        order.Status = "Confirmed";
                        order.ModifiedOn = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Order {OrderId} status updated to Confirmed", orderEvent.OrderId);
                    }

                    EventsConsumedCounter.Add(1);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer shutting down");
        }
        finally
        {
            consumer.Close();
        }
    }
}
