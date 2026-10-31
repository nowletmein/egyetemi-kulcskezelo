using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;

namespace KulcsRendszer.DataContext.Dtos {
    public class BuildingDto {
        public int Id { get; set; }
        public string BuildingCode { get; set; } = "";
        public string BuildingName { get; set; } = "";

        public ICollection<FloorDto> Floors { get; set; } = new List<FloorDto>();
    }

    public class BuildingInfoDto {
        public int Id { get; set; }
        public string BuildingCode { get; set; } = "";
        public string BuildingName { get; set; } = "";
    }

    public class BuildingCreateDto {
        public string BuildingCode { get; set; } = "";
        public string BuildingName { get; set; } = "";
    }

    public class BuildingUpdateDto {
        public string BuildingCode { get; set; } = "";
        public string BuildingName { get; set; } = "";
    }

    public class BuildingDeleteDto {
        public int Id { get; set; }
    }

}
