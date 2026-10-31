using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Dtos {

    public class FloorDto {
        public int Id { get; set; }
        public int Number { get; set; }
        public int BuildingId { get; set; }

        public ICollection<RoomResponse> Rooms { get; set; } = new List<RoomResponse>();
    }

    public class FloorInfoDto {
        public int Id { get; set; }
        public int Number { get; set; }
        public int BuildingId { get; set; }
    }

    public class FloorCreateDto {
        public int Number { get; set; }
        public int BuildingId { get; set; }
    }

    public class FloorUpdateDto {
        public int Number { get; set; }
        public int BuildingId { get; set; }
    }

    public class FloorDeleteDto {
        public int Id { get; set; }
    }
}