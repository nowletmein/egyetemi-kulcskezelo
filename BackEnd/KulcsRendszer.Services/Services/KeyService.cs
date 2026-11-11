using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.Services.Services {
    public interface IKeyService {
        Task<List<KeyDto>> GetAll();
        Task<KeyDto> Get(int keyId);
        Task<KeyDto> Create(KeyDto keyDto);
        Task<KeyDto> Update(int keyId, KeyDto keyDto);
        Task<bool> Delete(int keyId);
        Task<MasterKeyPermission> GrantMasterKeyPermission(
            int keyId,
            int userId,
            int grantedByAdminId);
        Task<bool> RevokeMasterKeyPermission(int keyId, int userId);
    }

    public class KeyService : IKeyService {
        private readonly KulcsRendszerDbContext _context;

        public KeyService(KulcsRendszerDbContext context) {
            _context = context;
        }

        private KeyDto MapToKeyDto(Key key) {
            return new KeyDto {
                Id = key.Id,
                RoomId = key.RoomId,
                IsMasterKey = key is MasterKey,
                MasterKeyName = key is MasterKey masterKey ? masterKey.MasterKeyName : "",
                AccessibleRoomIds = key is MasterKey accessibleMasterKey
                    ? accessibleMasterKey.AccessibleRooms.Select(room => room.Id).ToList()
                    : new List<string>(),
                MasterKeyPermissions = key.MasterKeyPermissions?
                    .Select(permission => new MasterKeyPermission {
                        Id = permission.Id,
                        UserId = permission.UserId,
                        KeyId = permission.KeyId,
                        GrantedByAdminId = permission.GrantedByAdminId,
                        GrantedAt = permission.GrantedAt
                    })
                    .ToList() ?? new List<MasterKeyPermission>(),
                KeyMovements = key.KeyMovements?
                    .Select(movement => new KeyMovement {
                        Id = movement.Id,
                        KeyId = movement.KeyId,
                        UserId = movement.UserId,
                        BookingId = movement.BookingId,
                        Timestamp = movement.Timestamp,
                        IdentificationMethod = movement.IdentificationMethod
                    })
                    .ToList() ?? new List<KeyMovement>(),
                IssueTickets = key.IssueTickets?
                    .Select(ticket => new IssueTicket {
                        Id = ticket.Id,
                        KeyId = ticket.KeyId,
                        RoomId = ticket.RoomId,
                        ReporterId = ticket.ReporterId,
                        Description = ticket.Description,
                        Status = ticket.Status
                    })
                    .ToList() ?? new List<IssueTicket>()
            };
        }

        private async Task<Key> MapToKey(KeyDto keyDto) {
            if (!keyDto.IsMasterKey) {
                return new Key {
                    RoomId = keyDto.RoomId
                };
            }

            var accessibleRoomIds = keyDto.AccessibleRoomIds?.Distinct().ToList()
                ?? new List<string>();
            var accessibleRooms = await _context.ClassRooms
                .Where(room => accessibleRoomIds.Contains(room.Id))
                .ToListAsync();

            if (accessibleRooms.Count != accessibleRoomIds.Count) {
                throw new RoomNotFoundException(
                    "One or more accessible rooms were not found.");
            }

            return new MasterKey {
                MasterKeyName = keyDto.MasterKeyName,
                AccessibleRooms = accessibleRooms
            };
        }

        public async Task<List<KeyDto>> GetAll() {
            var keys = await _context.Keys
                .Include(key => key.MasterKeyPermissions)
                .Include(key => key.KeyMovements)
                .Include(key => key.IssueTickets)
                .Include(key => ((MasterKey)key).AccessibleRooms)
                .OrderBy(key => key.Id)
                .ToListAsync();

            return keys.Select(MapToKeyDto).ToList();
        }

        public async Task<KeyDto> Get(int keyId) {
            var key = await _context.Keys
                .Include(item => item.MasterKeyPermissions)
                .Include(item => item.KeyMovements)
                .Include(item => item.IssueTickets)
                .Include(item => ((MasterKey)item).AccessibleRooms)
                .FirstOrDefaultAsync(item => item.Id == keyId);

            if (key == null) {
                throw new KeysNotFoundException($"Key not found with id: {keyId}");
            }

            return MapToKeyDto(key);
        }

        public async Task<KeyDto> Create(KeyDto keyDto) {
            if (keyDto.RoomId != null) {
                var roomExists = await _context.ClassRooms
                    .AnyAsync(room => room.Id == keyDto.RoomId);

                if (!roomExists) {
                    throw new RoomNotFoundException(
                        $"Room not found with id: {keyDto.RoomId}");
                }
            }

            if (keyDto.IsMasterKey && string.IsNullOrWhiteSpace(keyDto.MasterKeyName)) {
                throw new ArgumentException("A master key name is required.");
            }

            var key = await MapToKey(keyDto);
            _context.Keys.Add(key);
            await _context.SaveChangesAsync();

            return await Get(key.Id);
        }

        public async Task<KeyDto> Update(int keyId, KeyDto keyDto) {
            var key = await _context.Keys
                .Include(item => item.MasterKeyPermissions)
                .Include(item => item.KeyMovements)
                .Include(item => item.IssueTickets)
                .Include(item => ((MasterKey)item).AccessibleRooms)
                .FirstOrDefaultAsync(item => item.Id == keyId);

            if (key == null) {
                throw new KeysNotFoundException($"Key not found with id: {keyId}");
            }

            if (key is MasterKey masterKey) {
                if (!keyDto.IsMasterKey) {
                    throw new ArgumentException(
                        "A master key cannot be changed into a standard key.");
                }

                if (string.IsNullOrWhiteSpace(keyDto.MasterKeyName)) {
                    throw new ArgumentException("A master key name is required.");
                }

                var accessibleRoomIds = keyDto.AccessibleRoomIds?.Distinct().ToList()
                    ?? new List<string>();
                var accessibleRooms = await _context.ClassRooms
                    .Where(room => accessibleRoomIds.Contains(room.Id))
                    .ToListAsync();

                if (accessibleRooms.Count != accessibleRoomIds.Count) {
                    throw new RoomNotFoundException(
                        "One or more accessible rooms were not found.");
                }

                masterKey.MasterKeyName = keyDto.MasterKeyName;
                masterKey.AccessibleRooms = accessibleRooms;
            }
            else if (keyDto.IsMasterKey) {
                throw new ArgumentException(
                    "A standard key cannot be changed into a master key.");
            }
            else if (keyDto.RoomId != null) {
                var roomExists = await _context.ClassRooms
                    .AnyAsync(room => room.Id == keyDto.RoomId);

                if (!roomExists) {
                    throw new RoomNotFoundException(
                        $"Room not found with id: {keyDto.RoomId}");
                }
            }

            if (key is not MasterKey) {
                key.RoomId = keyDto.RoomId;
            }
            await _context.SaveChangesAsync();
            return MapToKeyDto(key);
        }

        public async Task<bool> Delete(int keyId) {
            var key = await _context.Keys.FindAsync(keyId);

            if (key == null) {
                throw new KeysNotFoundException($"Key not found with id: {keyId}");
            }

            _context.Keys.Remove(key);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MasterKeyPermission> GrantMasterKeyPermission(
            int keyId,
            int userId,
            int grantedByAdminId) {
            var isMasterKey = await _context.MasterKeys
                .AnyAsync(key => key.Id == keyId);

            if (!isMasterKey) {
                throw new KeysNotFoundException(
                    $"Master key not found with id: {keyId}");
            }

            var userExists = await _context.Users
                .AnyAsync(user => user.Id == userId);

            if (!userExists) {
                throw new UserNotFoundException($"User not found with id: {userId}");
            }

            var adminExists = await _context.Users
                .AnyAsync(user => user.Id == grantedByAdminId);

            if (!adminExists) {
                throw new UserNotFoundException(
                    $"Admin user not found with id: {grantedByAdminId}");
            }

            var permissionExists = await _context.MasterKeyPermissions
                .AnyAsync(permission =>
                    permission.KeyId == keyId &&
                    permission.UserId == userId);

            if (permissionExists) {
                throw new KeysAlreadyExistsException(
                    $"User {userId} already has permission for master key {keyId}.");
            }

            var permission = new MasterKeyPermission {
                KeyId = keyId,
                UserId = userId,
                GrantedByAdminId = grantedByAdminId,
                GrantedAt = DateTime.UtcNow
            };

            _context.MasterKeyPermissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<bool> RevokeMasterKeyPermission(int keyId, int userId) {
            var isMasterKey = await _context.MasterKeys
                .AnyAsync(key => key.Id == keyId);

            if (!isMasterKey) {
                throw new KeysNotFoundException(
                    $"Master key not found with id: {keyId}");
            }

            var permission = await _context.MasterKeyPermissions
                .FirstOrDefaultAsync(item =>
                    item.KeyId == keyId &&
                    item.UserId == userId);

            if (permission == null) {
                throw new KeysNotFoundException(
                    $"Permission for user {userId} and master key {keyId} was not found.");
            }

            _context.MasterKeyPermissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
