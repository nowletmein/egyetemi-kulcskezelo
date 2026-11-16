using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IIssueTicketService {
        Task<IReadOnlyList<IssueTicketDto>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<IssueTicketDto> CreateAsync(
            IssueTicketCreateDto issueTicketCreateDto,
            int reporterId,
            CancellationToken cancellationToken);
    }

    public class IssueTicketService : IIssueTicketService {
        private const string OpenStatus = "Open";

        private readonly KulcsRendszerDbContext _context;

        public IssueTicketService(KulcsRendszerDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<IssueTicketDto>> GetAllAsync(
            CancellationToken cancellationToken) {
            return await _context.IssueTickets
                .AsNoTracking()
                .OrderByDescending(ticket => ticket.Id)
                .Select(ticket => new IssueTicketDto {
                    Id = ticket.Id,
                    KeyId = ticket.KeyId,
                    RoomId = ticket.RoomId,
                    ReporterId = ticket.ReporterId,
                    Description = ticket.Description,
                    Status = ticket.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IssueTicketDto> CreateAsync(
            IssueTicketCreateDto issueTicketCreateDto,
            int reporterId,
            CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(issueTicketCreateDto.Description)) {
                throw new ArgumentException(
                    "The issue description must not be empty.");
            }

            var reporterExists = await _context.Users
                .AnyAsync(user => user.Id == reporterId, cancellationToken);

            if (!reporterExists) {
                throw new UserNotFoundException(
                    "User not found with this Id");
            }

            var roomExists = await _context.ClassRooms
                .AnyAsync(
                    room => room.Id == issueTicketCreateDto.RoomId,
                    cancellationToken);

            if (!roomExists) {
                throw new ClassRoomNotFoundException(
                    "Classroom not found with this Id");
            }

            if (issueTicketCreateDto.KeyId.HasValue) {
                var keyExists = await _context.Keys
                    .AnyAsync(
                        key => key.Id == issueTicketCreateDto.KeyId.Value,
                        cancellationToken);

                if (!keyExists) {
                    throw new KeysNotFoundException(
                        "Key not found with this Id");
                }
            }

            var ticket = new IssueTicket {
                KeyId = issueTicketCreateDto.KeyId,
                RoomId = issueTicketCreateDto.RoomId,
                ReporterId = reporterId,
                Description = issueTicketCreateDto.Description.Trim(),
                Status = OpenStatus
            };

            _context.IssueTickets.Add(ticket);
            await _context.SaveChangesAsync(cancellationToken);

            return ToDto(ticket);
        }

        private static IssueTicketDto ToDto(IssueTicket ticket) => new IssueTicketDto {
            Id = ticket.Id,
            KeyId = ticket.KeyId,
            RoomId = ticket.RoomId,
            ReporterId = ticket.ReporterId,
            Description = ticket.Description,
            Status = ticket.Status
        };
    }
}
