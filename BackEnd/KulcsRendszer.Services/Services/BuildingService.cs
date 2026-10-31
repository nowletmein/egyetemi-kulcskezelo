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
    public interface IBuildingService {
        Task<BuildingDto> Get(int buildingId);
        Task<BuildingInfoDto> GetInfo(int buildingId);
        Task<BuildingDto> Create(BuildingCreateDto buildingCreateDto);
        Task<BuildingDto> Update(int buildingId, BuildingUpdateDto buildingUpdateDto);
        Task<bool> Delete(int buildingId);
    }

    public class BuildingService : IBuildingService {
        private readonly KulcsRendszerDbContext _context;

        public BuildingService(KulcsRendszerDbContext context) {
            _context = context;
        }

        private BuildingDto MapToBuildingDto(Building building) {
            return new BuildingDto {
                Id = building.Id,
                BuildingCode = building.BuildingCode,
                BuildingName = building.BuildingName,
                Floors = building.Floors?.Select(f => new FloorDto {
                    Id = f.Id,
                    Number = f.Number,
                    BuildingId = f.BuildingId,
                    Rooms = f.Rooms?.Select(r => new RoomResponse(
                        r.Id,
                        building.BuildingName,
                        r.FloorId,
                        r.Name,
                        r.Capacity,
                        r.Equipment
                    )).ToList() ?? new List<RoomResponse>()
                }).ToList() ?? new List<FloorDto>()
            };
        }

        private Building MapToBuilding(BuildingCreateDto buildingCreateDto) {
            return new Building {
                BuildingCode = buildingCreateDto.BuildingCode,
                BuildingName = buildingCreateDto.BuildingName
            };
        }

        public async Task<BuildingDto> Get(int buildingId) {
            var building = await _context.Buildings
                .Include(b => b.Floors)
                    .ThenInclude(f => f.Rooms)
                .FirstOrDefaultAsync(b => b.Id == buildingId);

            if (building == null) {
                throw new BuildingNotFoundException($"Building not found with id: {buildingId}");
            }

            return MapToBuildingDto(building);
        }

        public async Task<BuildingInfoDto> GetInfo(int buildingId) {
            var building = await _context.Buildings
                .FirstOrDefaultAsync(b => b.Id == buildingId);

            if (building == null) {
                throw new BuildingNotFoundException($"Building not found with id: {buildingId}");
            }

            return new BuildingInfoDto {
                Id = building.Id,
                BuildingCode = building.BuildingCode,
                BuildingName = building.BuildingName
            };
        }

        public async Task<BuildingDto> Create(BuildingCreateDto buildingCreateDto) {
            bool exists = await _context.Buildings.AnyAsync(b =>
                b.BuildingCode == buildingCreateDto.BuildingCode ||
                b.BuildingName == buildingCreateDto.BuildingName);

            if (exists) {
                throw new BuildingAlreadyExistsException("Building with this BuildingCode or BuildingName already exists.");
            }

            var building = MapToBuilding(buildingCreateDto);
            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();

            return MapToBuildingDto(building);
        }

        public async Task<BuildingDto> Update(int buildingId, BuildingUpdateDto buildingUpdateDto) {
            var building = await _context.Buildings
                .Include(b => b.Floors)
                    .ThenInclude(f => f.Rooms)
                .FirstOrDefaultAsync(b => b.Id == buildingId);

            if (building == null) {
                throw new BuildingNotFoundException($"Building not found with id: {buildingId}");
            }

            if (!string.IsNullOrWhiteSpace(buildingUpdateDto.BuildingCode) && buildingUpdateDto.BuildingCode != "string") {
                building.BuildingCode = buildingUpdateDto.BuildingCode;
            }

            if (!string.IsNullOrWhiteSpace(buildingUpdateDto.BuildingName) && buildingUpdateDto.BuildingName != "string") {
                building.BuildingName = buildingUpdateDto.BuildingName;
            }

            bool nameOrCodeConflict = await _context.Buildings.AnyAsync(b =>
                b.Id != buildingId &&
                (b.BuildingCode == building.BuildingCode || b.BuildingName == building.BuildingName));

            if (nameOrCodeConflict) {
                throw new BuildingAlreadyExistsException("Another building with this BuildingCode or BuildingName already exists.");
            }

            await _context.SaveChangesAsync();
            return MapToBuildingDto(building);
        }

        public async Task<bool> Delete(int buildingId) {
            var building = await _context.Buildings.FindAsync(buildingId);

            if (building == null) {
                throw new BuildingNotFoundException($"Building not found with id: {buildingId}");
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
