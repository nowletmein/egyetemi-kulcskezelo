using KulcsRendszer.DataContext.Context;
using KulcsRendszer.Services.Services;
using Microsoft.EntityFrameworkCore;
public class UnreturnedKeyMonitor : BackgroundService {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnreturnedKeyMonitor> _logger;

    public UnreturnedKeyMonitor(
        IServiceScopeFactory scopeFactory,
        ILogger<UnreturnedKeyMonitor> logger) {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var scope = _scopeFactory.CreateScope();
                var userService = scope.ServiceProvider
                .GetRequiredService<IUserService>();
                var context = scope.ServiceProvider
                    .GetRequiredService<KulcsRendszerDbContext>();

                var cutoff = DateTime.UtcNow.AddMinutes(-10);

                var bookings = await context.Bookings
                    .Include(b => b.KeyMovements)
                    .Where(b => b.EndTime <= cutoff)
                    .ToListAsync(stoppingToken);

                foreach (var booking in bookings) {
                    foreach (var movement in booking.KeyMovements) {
                        if (movement.Status == "given out") {
                            movement.Status = "unreturned";

                            if (movement.UserId is int userId) {
                                await userService.AddNotification(
                                    userId,
                                    $"Figyelem! A(z) {booking.Id}. foglaláshoz tartozó " +
                                    $"kulcsot ({movement.KeyId}) nem adtad le a foglalás " +
                                    "végétől számított 10 percen belül. " +
                                    "Kérjük, vedd fel a kapcsolatot a portával."
                                );

                                _logger.LogWarning(
                                    "Unreturned key notification sent. UserId: {UserId}, BookingId: {BookingId}, KeyId: {KeyId}",
                                    userId,
                                    booking.Id,
                                    movement.KeyId
                                );
                            }
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "Error while checking unreturned keys.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }
}
