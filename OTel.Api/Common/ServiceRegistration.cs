using Confluent.Kafka;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OTel.Api.BackgroundServices;
using OTel.Application.Interfaces;
using OTel.Application.Services;
using OTel.Domain.Interfaces.Common;
using OTel.Infrastructure.Context;
using OTel.Infrastructure.Messaging;

namespace OTel.Api.Common;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();

        // Kafka
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        services.AddSingleton(new ProducerConfig { BootstrapServers = bootstrapServers });
        services.AddSingleton<IEventProducer, KafkaEventProducer>();

        services.AddSingleton(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = configuration["Kafka:ConsumerGroup"] ?? "order-events-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        });
        services.AddHostedService<OrderEventConsumerService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateProductValidator>();

        // Exception handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
