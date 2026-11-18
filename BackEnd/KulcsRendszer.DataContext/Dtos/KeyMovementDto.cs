using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Dtos {
    public class KeyMovementDto {
        public int? KeyId { get; set; }
        public int? UserId { get; set; }
        public string IdentificationMethod { get; set; }
        public DateTime Timestamp { get; set; }
        public int? BookingId { get; set; }
        public string Status { get; set; }
    }
}
