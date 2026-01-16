namespace WorkPlacementWorker;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;

    public class Worker : BackgroundService
    {

        public const string ActivitySourceName = "demo-worker.activity";
        public const string MeterName = "demo-worker.meter";

        private static readonly ActivitySource ActivitySource = new(ActivitySourceName); // For tempo, activity source creates spans
        private static readonly Meter Meter = new(MeterName); // Prometheus, metrics registry, doesn't create anything just holds metrics
    private static readonly Counter<long> TickCounter = Meter.CreateCounter<long>("worker_ticks_total"); // Prometheus, counter metric, Prometheus scrapes this value 

        private readonly ILogger<Worker> _logger; // Loki 
        private readonly IHttpClientFactory _httpClientFactory; // For one-off tracer to access internet 

    // NEW: one-off guard for tracer
    private bool _oneOffTraceDone = false;
        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

    // Main 5 second loop sending to Prometheus, Loki and Tempo
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // NEW Run the demo trace once, then continue as normal
            if (!_oneOffTraceDone)
            {
                _oneOffTraceDone = true;
                await RunOneOffDemoTrace(stoppingToken);
            }
            while (!stoppingToken.IsCancellationRequested)
                {
                    using var activity = ActivitySource.StartActivity("TickWork");
                    activity?.SetTag("demo.tag", "hello");

                    _logger.LogInformation("Worker tick at {time}", DateTimeOffset.UtcNow);

                    TickCounter.Add(1);

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
        }

    // For 3 part one-off tracer
    private async Task RunOneOffDemoTrace(CancellationToken ct)
    {
        // Parent span
        using var trace = ActivitySource.StartActivity("OneOffDemoTrace", ActivityKind.Internal);
        trace?.SetTag("demo.trace", "startup-three-part");

        // Span 1: “prepare”
        using (var span1 = ActivitySource.StartActivity("Step1_Prepare", ActivityKind.Internal))
        {
            span1?.SetTag("step", 1);
            _logger.LogInformation("One-off trace: Step 1 prepare");
            await Task.Delay(150, ct);
        }

        // Span 2: HTTP call (this will show as an HTTP client span too if you enabled AddHttpClientInstrumentation)
        using (var span2 = ActivitySource.StartActivity("Step2_HTTP", ActivityKind.Client))
        {
            span2?.SetTag("step", 2);

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Use a simple GET endpoint (Google often blocks bots; Yahoo can too)
                // This one is super reliable:
                var url = "https://example.com";

                _logger.LogInformation("One-off trace: Step 2 HTTP GET {url}", url);

                using var resp = await client.GetAsync(url, ct);
                span2?.SetTag("http.status_code", (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                span2?.SetTag("error", true);
                _logger.LogError(ex, "One-off trace: HTTP call failed");
            }
        }

        // Span 3: “finalize”
        using (var span3 = ActivitySource.StartActivity("Step3_Finalize", ActivityKind.Internal))
        {
            span3?.SetTag("step", 3);
            _logger.LogInformation("One-off trace: Step 3 finalize");
            await Task.Delay(100, ct);
        }

        _logger.LogInformation("One-off trace complete");
    }
}

