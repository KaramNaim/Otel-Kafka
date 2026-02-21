using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OTel.Application.Interfaces;

namespace OTel.Infrastructure.Messaging;

public class KafkaEventProducer : IEventProducer, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("OTel.Kafka");
    private static readonly Meter Meter = new("OTel.Kafka");
    private static readonly Counter<long> EventsPublishedCounter = Meter.CreateCounter<long>("orders.events.published", "count", "Number of order events published to Kafka");

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;

    public KafkaEventProducer(ProducerConfig config, ILogger<KafkaEventProducer> logger)
    {
        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("Kafka.Produce", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", topic);
        activity?.SetTag("messaging.kafka.message.key", key);

        var json = JsonSerializer.Serialize(message);

        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = json
            }, cancellationToken);

            activity?.SetTag("messaging.kafka.partition", result.Partition.Value);
            activity?.SetTag("messaging.kafka.offset", result.Offset.Value);
            EventsPublishedCounter.Add(1);

            _logger.LogInformation("Published message to {Topic} [partition {Partition}, offset {Offset}]",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to publish message to {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
