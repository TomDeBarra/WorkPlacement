using WorkPlacementWorker;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);
// Name your service (this is what shows up in Grafana/Tempo)
const string serviceName = "demo-worker";

builder.Services.AddHttpClient(); //New thing for new tracer

// ----- LOGGING (to Loki via Otel Collector) -----
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService(serviceName));

    options.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri("http://otel-collector:4317");
    });
});

// ----- TRACING (to Tempo via Otel Collector) -----
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Worker.ActivitySourceName)
            .AddHttpClientInstrumentation() // NEW tracer
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("http://otel-collector:4317");
            });
    })

    // ----- METRICS (to Prometheus via Otel Collector) -----
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(Worker.MeterName)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("http://otel-collector:4317");
            });
    });

// New Tracer [Proper real tracer]

builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
