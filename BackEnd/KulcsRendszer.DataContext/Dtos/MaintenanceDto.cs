using System;
using System.Collections.Generic;

namespace KulcsRendszer.DataContext.Dtos {
    public class MaintenanceCreateDto {
        public string RoomId { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class MaintenanceDto {
        public int Id { get; set; }
        public string RoomId { get; set; } = "";
        public int AdminId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class MaintenanceCreateResultDto {
        public MaintenanceDto Maintenance { get; set; } = new();
        public IReadOnlyList<BookingDto> AffectedBookings { get; set; } =
            Array.Empty<BookingDto>();
    }
}
