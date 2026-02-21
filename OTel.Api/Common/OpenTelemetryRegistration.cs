using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OTel.Api.Common;

public static class OpenTelemetryRegistration
{
    public static IServiceCollection AddOpenTelemetryConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "OTel.Api";
        var resource = ResourceBuilder.CreateDefault().AddService(serviceName);

        // Tracing
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("OTel.Api")
                    .AddSource("OTel.Kafka");

                // OTLP exporter for Jaeger
                if (configuration.GetValue<bool>("OpenTelemetry:Jaeger:Enabled"))
                {
                    var endpoint = configuration["OpenTelemetry:Jaeger:Endpoint"] ?? "http://localhost:4317";
                    tracing.AddOtlpExporter("jaeger", options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                    });
                }

                // OTLP exporter for Grafana (Tempo via OTel Collector)
                if (configuration.GetValue<bool>("OpenTelemetry:Grafana:Enabled"))
                {
                    var endpoint = configuration["OpenTelemetry:Grafana:OtlpEndpoint"] ?? "http://localhost:4317";
                    tracing.AddOtlpExporter("grafana", options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                    });
                }

                // Console exporter for dev debugging
                if (configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter"))
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("OTel.Api")
                    .AddMeter("OTel.Kafka");

                // Prometheus endpoint for Grafana scraping
                if (configuration.GetValue<bool>("OpenTelemetry:Grafana:Enabled"))
                {
                    metrics.AddPrometheusExporter();
                }

                // OTLP metrics exporter for Jaeger/Datadog
                if (configuration.GetValue<bool>("OpenTelemetry:Jaeger:Enabled"))
                {
                    var endpoint = configuration["OpenTelemetry:Jaeger:Endpoint"] ?? "http://localhost:4317";
                    metrics.AddOtlpExporter("jaeger-metrics", options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                    });
                }

                // Console exporter for dev debugging
                if (configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter"))
                {
                    metrics.AddConsoleExporter();
                }
            })
            .WithLogging(logging =>
            {
                // OTLP log exporter for Jaeger
                if (configuration.GetValue<bool>("OpenTelemetry:Jaeger:Enabled"))
                {
                    var endpoint = configuration["OpenTelemetry:Jaeger:Endpoint"] ?? "http://localhost:4317";
                    logging.AddOtlpExporter("jaeger-logs", options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                    });
                }

                // OTLP log exporter for Loki (HTTP/Protobuf)
                if (configuration.GetValue<bool>("OpenTelemetry:Loki:Enabled"))
                {
                    var endpoint = configuration["OpenTelemetry:Loki:Endpoint"] ?? "http://localhost:3100/otlp/v1/logs";
                    logging.AddOtlpExporter("loki", options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });
                }

                if (configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter"))
                {
                    logging.AddConsoleExporter();
                }
            });

        return services;
    }

    public static WebApplication UseOpenTelemetryMiddleware(this WebApplication app)
    {
        var configuration = app.Configuration;

        // Expose Prometheus /metrics endpoint
        if (configuration.GetValue<bool>("OpenTelemetry:Grafana:Enabled"))
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        return app;
    }
}
