using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Enums;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface INoShowService {
        Task<int> MarkNoShowsAsync(
            DateTime now,
            TimeSpan gracePeriod,
            CancellationToken cancellationToken);
    }

    public class NoShowService : INoShowService {
        private readonly KulcsRendszerDbContext _context;

        public NoShowService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task<int> MarkNoShowsAsync(
            DateTime now,
            TimeSpan gracePeriod,
            CancellationToken cancellationToken) {
            var deadline = now - gracePeriod;

            var noShowBookings = await _context.Bookings
                .Where(booking =>
                    booking.Status == BookingStatuses.Active &&
                    booking.StartTime <= deadline &&
                    !booking.KeyMovements.Any())
                .ToListAsync(cancellationToken);

            foreach (var booking in noShowBookings) {
                booking.Status = BookingStatuses.NoShow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return noShowBookings.Count;
        }
    }
}
