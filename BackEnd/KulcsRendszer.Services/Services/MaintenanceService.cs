using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IMaintenanceService {
        Task<IReadOnlyList<MaintenanceDto>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<MaintenanceCreateResultDto> CreateAsync(
            MaintenanceCreateDto maintenanceCreateDto,
            int adminId,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            int maintenanceId,
            CancellationToken cancellationToken);
    }

    public class MaintenanceService : IMaintenanceService {
        private readonly KulcsRendszerDbContext _context;

        public MaintenanceService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<MaintenanceDto>> GetAllAsync(
            CancellationToken cancellationToken) {
            return await _context.Maintenances
                .AsNoTracking()
                .OrderBy(maintenance => maintenance.StartTime)
                .Select(maintenance => new MaintenanceDto {
                    Id = maintenance.Id,
                    RoomId = maintenance.RoomId,
                    AdminId = maintenance.AdminId,
                    StartTime = maintenance.StartTime,
                    EndTime = maintenance.EndTime
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<MaintenanceCreateResultDto> CreateAsync(
            MaintenanceCreateDto maintenanceCreateDto,
            int adminId,
            CancellationToken cancellationToken) {
            if (maintenanceCreateDto.StartTime >= maintenanceCreateDto.EndTime) {
                throw new ArgumentException(
                    "The maintenance start time must be earlier than the end time.");
            }

            var adminExists = await _context.Users
                .AnyAsync(user => user.Id == adminId, cancellationToken);

            if (!adminExists) {
                throw new UserNotFoundException(
                    "User not found with this Id");
            }

            var roomExists = await _context.ClassRooms
                .AnyAsync(
                    room => room.Id == maintenanceCreateDto.RoomId,
                    cancellationToken);

            if (!roomExists) {
                throw new ClassRoomNotFoundException(
                    "Classroom not found with this Id");
            }

            var hasOverlappingMaintenance = await _context.Maintenances
                .AnyAsync(
                    maintenance =>
                        maintenance.RoomId == maintenanceCreateDto.RoomId &&
                        maintenance.StartTime < maintenanceCreateDto.EndTime &&
                        maintenanceCreateDto.StartTime < maintenance.EndTime,
                    cancellationToken);

            if (hasOverlappingMaintenance) {
                throw new MaintenanceAlreadyExistsException(
                    "The classroom already has maintenance scheduled for the selected time.");
            }

            var newMaintenance = new Maintenance {
                RoomId = maintenanceCreateDto.RoomId,
                AdminId = adminId,
                StartTime = maintenanceCreateDto.StartTime,
                EndTime = maintenanceCreateDto.EndTime
            };

            _context.Maintenances.Add(newMaintenance);
            await _context.SaveChangesAsync(cancellationToken);

            var affectedBookings = await _context.Bookings
                .AsNoTracking()
                .Where(booking =>
                    booking.RoomId == maintenanceCreateDto.RoomId &&
                    booking.Status != BookingStatuses.NoShow &&
                    booking.StartTime < maintenanceCreateDto.EndTime &&
                    maintenanceCreateDto.StartTime < booking.EndTime)
                .OrderBy(booking => booking.StartTime)
                .Select(booking => new BookingDto {
                    Id = booking.Id,
                    UserId = booking.UserId,
                    RoomId = booking.RoomId,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Status = booking.Status,
                    SpecialRequest = booking.SpecialRequest
                })
                .ToListAsync(cancellationToken);

            return new MaintenanceCreateResultDto {
                Maintenance = ToDto(newMaintenance),
                AffectedBookings = affectedBookings
            };
        }

        public async Task DeleteAsync(
            int maintenanceId,
            CancellationToken cancellationToken) {
            var maintenance = await _context.Maintenances
                .FirstOrDefaultAsync(
                    item => item.Id == maintenanceId,
                    cancellationToken);

            if (maintenance == null) {
                throw new MaintenanceNotFoundException(
                    "Maintenance not found with this Id");
            }

            _context.Maintenances.Remove(maintenance);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static MaintenanceDto ToDto(Maintenance maintenance) => new MaintenanceDto {
            Id = maintenance.Id,
            RoomId = maintenance.RoomId,
            AdminId = maintenance.AdminId,
            StartTime = maintenance.StartTime,
            EndTime = maintenance.EndTime
        };
    }
}
