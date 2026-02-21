using OTel.Api.Common;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "OTel.Api")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

    var serviceName = context.Configuration["OpenTelemetry:ServiceName"] ?? "OTel.Api";

    // Serilog OpenTelemetry sink for Jaeger
    if (context.Configuration.GetValue<bool>("OpenTelemetry:Jaeger:Enabled"))
    {
        var endpoint = context.Configuration["OpenTelemetry:Jaeger:Endpoint"] ?? "http://localhost:4317";
        loggerConfig.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName
            };
        });
    }

    // Serilog OpenTelemetry sink for Loki (HTTP/Protobuf)
    if (context.Configuration.GetValue<bool>("OpenTelemetry:Loki:Enabled"))
    {
        var lokiEndpoint = context.Configuration["OpenTelemetry:Loki:Endpoint"] ?? "http://localhost:3100/otlp/v1/logs";
        loggerConfig.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = lokiEndpoint;
            options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName
            };
        });
    }
});

// Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddMapsterConfiguration();
builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);

var app = builder.Build();

// Middleware
app.UseApplicationMiddleware();
app.UseOpenTelemetryMiddleware();
app.MapControllers();

app.Run();
