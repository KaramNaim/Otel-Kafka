namespace OTel.Application.Interfaces;

public interface IEventProducer
{
    Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}
