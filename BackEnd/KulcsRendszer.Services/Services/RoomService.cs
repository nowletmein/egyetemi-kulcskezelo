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
    public interface IRoomService {
        Task<List<RoomResponse>> GetAll();
        Task<RoomResponse> Get(string roomId);
        Task<RoomInfoDto> GetInfo(string roomId);
        Task<RoomResponse> Create(RoomCreateDto roomCreateDto);
        Task<RoomResponse> Update(string roomId, RoomUpdateDto roomUpdateDto);
        Task<bool> Delete(string roomId);
        Task<IReadOnlyList<RoomResponse>?> GetByFloorAsync(int floorId, CancellationToken cancellationToken);
    }

    public sealed class RoomService : IRoomService {
        private readonly KulcsRendszerDbContext _dbContext;

        public RoomService(KulcsRendszerDbContext dbContext) {
            _dbContext = dbContext;
        }

        private static RoomResponse MapToRoomResponse(ClassRoom room) {
            return new RoomResponse {
                Id = room.Id,
                Building = room.Floor?.Building?.BuildingName ?? "",
                FloorId = room.FloorId,
                Name = room.Name,
                Capacity = room.Capacity,
                Equipment = room.Equipment
            };
        }

        private static RoomInfoDto MapToRoomInfoDto(ClassRoom room) {
            return new RoomInfoDto {
                Id = room.Id,
                FloorId = room.FloorId,
                Name = room.Name,
                Capacity = room.Capacity,
                Equipment = room.Equipment
            };
        }

        private static ClassRoom MapToRoom(RoomCreateDto roomCreateDto) {
            return new ClassRoom {
                Id = roomCreateDto.Id,
                FloorId = roomCreateDto.FloorId,
                Name = roomCreateDto.Name,
                Capacity = roomCreateDto.Capacity,
                Equipment = roomCreateDto.Equipment
            };
        }

        public async Task<List<RoomResponse>> GetAll() {
            var rooms = await _dbContext.ClassRooms
                .Include(room => room.Floor)
                    .ThenInclude(floor => floor.Building)
                .OrderBy(room => room.Name)
                .ToListAsync();

            return rooms.Select(MapToRoomResponse).ToList();
        }

        public async Task<RoomResponse> Get(string roomId) {
            var room = await _dbContext.ClassRooms
                .Include(room => room.Floor)
                    .ThenInclude(floor => floor.Building)
                .FirstOrDefaultAsync(room => room.Id == roomId);

            if (room == null) {
                throw new RoomNotFoundException($"Room not found with id: {roomId}");
            }

            return MapToRoomResponse(room);
        }

        public async Task<RoomInfoDto> GetInfo(string roomId) {
            var room = await _dbContext.ClassRooms
                .FirstOrDefaultAsync(room => room.Id == roomId);

            if (room == null) {
                throw new RoomNotFoundException($"Room not found with id: {roomId}");
            }

            return MapToRoomInfoDto(room);
        }

        public async Task<RoomResponse> Create(RoomCreateDto roomCreateDto) {
            var floorExists = await _dbContext.Floors
                .AnyAsync(floor => floor.Id == roomCreateDto.FloorId);

            if (!floorExists) {
                throw new FloorNotFoundException($"Floor not found with id: {roomCreateDto.FloorId}");
            }

            var idExists = await _dbContext.ClassRooms
                .AnyAsync(room => room.Id == roomCreateDto.Id);
            var nameExists = await _dbContext.ClassRooms.AnyAsync(room =>
                room.FloorId == roomCreateDto.FloorId &&
                room.Name == roomCreateDto.Name);

            if (idExists || nameExists) {
                throw new RoomAlreadyExistsException("A room with this id or name already exists.");
            }

            var room = MapToRoom(roomCreateDto);
            _dbContext.ClassRooms.Add(room);
            await _dbContext.SaveChangesAsync();

            await _dbContext.Entry(room)
                .Reference(classRoom => classRoom.Floor)
                .Query()
                .Include(floor => floor.Building)
                .LoadAsync();

            return MapToRoomResponse(room);
        }

        public async Task<RoomResponse> Update(string roomId, RoomUpdateDto roomUpdateDto) {
            var room = await _dbContext.ClassRooms
                .Include(classRoom => classRoom.Floor)
                    .ThenInclude(floor => floor.Building)
                .FirstOrDefaultAsync(classRoom => classRoom.Id == roomId);

            if (room == null) {
                throw new RoomNotFoundException($"Room not found with id: {roomId}");
            }

            var floorChanged = roomUpdateDto.FloorId > 0 && room.FloorId != roomUpdateDto.FloorId;
            if (roomUpdateDto.FloorId > 0) {
                var floorExists = await _dbContext.Floors
                    .AnyAsync(floor => floor.Id == roomUpdateDto.FloorId);

                if (!floorExists) {
                    throw new FloorNotFoundException($"Floor not found with id: {roomUpdateDto.FloorId}");
                }

                room.FloorId = roomUpdateDto.FloorId;
            }

            room.Name = roomUpdateDto.Name;
            room.Capacity = roomUpdateDto.Capacity;
            room.Equipment = roomUpdateDto.Equipment;

            var nameConflict = await _dbContext.ClassRooms.AnyAsync(otherRoom =>
                otherRoom.Id != roomId &&
                otherRoom.FloorId == room.FloorId &&
                otherRoom.Name == room.Name);

            if (nameConflict) {
                throw new RoomAlreadyExistsException("Another room with this name already exists on this floor.");
            }

            await _dbContext.SaveChangesAsync();

            if (floorChanged) {
                return await _dbContext.ClassRooms
                    .Where(classRoom => classRoom.Id == roomId)
                    .Select(classRoom => new RoomResponse {
                        Id = classRoom.Id,
                        Building = classRoom.Floor.Building.BuildingName,
                        FloorId = classRoom.FloorId,
                        Name = classRoom.Name,
                        Capacity = classRoom.Capacity,
                        Equipment = classRoom.Equipment
                    })
                    .FirstAsync();
            }

            return MapToRoomResponse(room);
        }

        public async Task<bool> Delete(string roomId) {
            var room = await _dbContext.ClassRooms
                .FirstOrDefaultAsync(classRoom => classRoom.Id == roomId);

            if (room == null) {
                throw new RoomNotFoundException($"Room not found with id: {roomId}");
            }

            _dbContext.ClassRooms.Remove(room);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<RoomResponse>?> GetByFloorAsync(
            int floorId,
            CancellationToken cancellationToken) {
            var floorExists = await _dbContext.Floors
                .AnyAsync(floor => floor.Id == floorId, cancellationToken);

            if (!floorExists) {
                return null;
            }

            return await _dbContext.ClassRooms
                .Where(room => room.FloorId == floorId)
                .OrderBy(room => room.Name)
                .Select(room => new RoomResponse {
                    Id = room.Id,
                    Building = room.Floor.Building.BuildingName,
                    FloorId = room.FloorId,
                    Name = room.Name,
                    Capacity = room.Capacity,
                    Equipment = room.Equipment
                })
                .ToListAsync(cancellationToken);
        }
    }
}
