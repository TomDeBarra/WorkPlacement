namespace WorkPlacementWorker;

using System.Diagnostics;
using System.Diagnostics.Metrics;

    public class Worker : BackgroundService
    {

        public const string ActivitySourceName = "demo-worker.activity";
        public const string MeterName = "demo-worker.meter";

        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
        private static readonly Meter Meter = new(MeterName);
        private static readonly Counter<long> TickCounter = Meter.CreateCounter<long>("worker_ticks_total");

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

    /* Original
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    */

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var activity = ActivitySource.StartActivity("TickWork");
                activity?.SetTag("demo.tag", "hello");

                _logger.LogInformation("Worker tick at {time}", DateTimeOffset.UtcNow);

                TickCounter.Add(1);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

