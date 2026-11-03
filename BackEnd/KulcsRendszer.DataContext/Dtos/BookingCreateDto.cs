using System;

namespace KulcsRendszer.DataContext.Dtos {
    public class BookingCreateDto {
        public int UserId { get; set; }
        public string RoomId { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string SpecialRequest { get; set; } = "";
    }
}
