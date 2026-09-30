using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Entities {
    public class KeyMovement {
        public int Id { get; set; }
        public int KeyId { get; set; }
        public Keys Keys { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }
        public string Type { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string IdentificationMethod { get; set; } = "";
    }
}