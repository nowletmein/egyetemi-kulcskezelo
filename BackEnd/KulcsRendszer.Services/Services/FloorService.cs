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
    public interface IFloorService {
        Task<List<FloorDto>> GetAll();
        Task<FloorDto> Get(int floorId);
        Task<FloorInfoDto> GetInfo(int floorId);
        Task<FloorDto> Create(FloorCreateDto floorCreateDto);
        Task<FloorDto> Update(int floorId, FloorUpdateDto floorUpdateDto);
        Task<bool> Delete(int floorId);
    }

    public class FloorService : IFloorService {
        private readonly KulcsRendszerDbContext _context;

        public FloorService(KulcsRendszerDbContext context) {
            _context = context;
        }

        private FloorDto MapToFloorDto(Floor floor) {
            return new FloorDto {
                Id = floor.Id,
                Number = floor.Number,
                BuildingId = floor.BuildingId,
                Rooms = floor.Rooms?.Select(r => new RoomResponse(
                    r.Id,
                    floor.Building?.BuildingName ?? "",
                    r.FloorId,
                    r.Name,
                    r.Capacity,
                    r.Equipment
                )).ToList() ?? new List<RoomResponse>()
            };
        }

        private Floor MapToFloor(FloorCreateDto floorCreateDto) {
            return new Floor {
                Number = floorCreateDto.Number,
                BuildingId = floorCreateDto.BuildingId
            };
        }

        public async Task<List<FloorDto>> GetAll() {
            var floors = await _context.Floors
                .Include(f => f.Building)
                .Include(f => f.Rooms)
                .ToListAsync();

            return floors.Select(MapToFloorDto).ToList();
        }

        public async Task<FloorDto> Get(int floorId) {
            var floor = await _context.Floors
                .Include(f => f.Building)
                .Include(f => f.Rooms)
                .FirstOrDefaultAsync(f => f.Id == floorId);

            if (floor == null) {
                throw new FloorNotFoundException($"Floor not found with id: {floorId}");
            }

            return MapToFloorDto(floor);
        }

        public async Task<FloorInfoDto> GetInfo(int floorId) {
            var floor = await _context.Floors
                .FirstOrDefaultAsync(f => f.Id == floorId);

            if (floor == null) {
                throw new FloorNotFoundException($"Floor not found with id: {floorId}");
            }

            return new FloorInfoDto {
                Id = floor.Id,
                Number = floor.Number,
                BuildingId = floor.BuildingId
            };
        }

        public async Task<FloorDto> Create(FloorCreateDto floorCreateDto) {
            var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == floorCreateDto.BuildingId);
            if (!buildingExists) {
                throw new BuildingNotFoundException($"Building not found with id: {floorCreateDto.BuildingId}");
            }

            bool exists = await _context.Floors.AnyAsync(f =>
                f.BuildingId == floorCreateDto.BuildingId &&
                f.Number == floorCreateDto.Number);

            if (exists) {
                throw new FloorAlreadyExistsException("Floor with this number already exists in the given building.");
            }

            var floor = MapToFloor(floorCreateDto);
            _context.Floors.Add(floor);
            await _context.SaveChangesAsync();

            await _context.Entry(floor).Reference(f => f.Building).LoadAsync();

            return MapToFloorDto(floor);
        }

        public async Task<FloorDto> Update(int floorId, FloorUpdateDto floorUpdateDto) {
            var floor = await _context.Floors
                .Include(f => f.Building)
                .Include(f => f.Rooms)
                .FirstOrDefaultAsync(f => f.Id == floorId);

            if (floor == null) {
                throw new FloorNotFoundException($"Floor not found with id: {floorId}");
            }

            if (floorUpdateDto.BuildingId > 0) {
                var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == floorUpdateDto.BuildingId);
                if (!buildingExists) {
                    throw new BuildingNotFoundException($"Building not found with id: {floorUpdateDto.BuildingId}");
                }
                floor.BuildingId = floorUpdateDto.BuildingId;
            }

            floor.Number = floorUpdateDto.Number;

            bool numberConflict = await _context.Floors.AnyAsync(f =>
                f.Id != floorId &&
                f.BuildingId == floor.BuildingId &&
                f.Number == floor.Number);

            if (numberConflict) {
                throw new FloorAlreadyExistsException("Another floor with this number already exists in this building.");
            }

            await _context.SaveChangesAsync();
            return MapToFloorDto(floor);
        }

        public async Task<bool> Delete(int floorId) {
            var floor = await _context.Floors.FindAsync(floorId);

            if (floor == null) {
                throw new FloorNotFoundException($"Floor not found with id: {floorId}");
            }

            _context.Floors.Remove(floor);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
