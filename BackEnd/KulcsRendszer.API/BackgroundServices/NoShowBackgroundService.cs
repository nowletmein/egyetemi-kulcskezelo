using KulcsRendszer.Services.Services;

namespace KulcsRendszer.API.BackgroundServices {
    public class NoShowBackgroundService : BackgroundService {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NoShowBackgroundService> _logger;
        private readonly TimeSpan _gracePeriod;

        public NoShowBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<NoShowBackgroundService> logger) {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _gracePeriod = TimeSpan.FromMinutes(
                configuration.GetValue("NoShow:GracePeriodMinutes", 15));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            do {
                try {
                    using var scope = _scopeFactory.CreateScope();
                    var noShowService = scope.ServiceProvider
                        .GetRequiredService<INoShowService>();

                    var count = await noShowService.MarkNoShowsAsync(
                        DateTime.Now,
                        _gracePeriod,
                        stoppingToken);

                    if (count > 0) {
                        _logger.LogInformation("{Count} booking(s) marked as no-show.", count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    _logger.LogError(ex, "No-show check failed.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
