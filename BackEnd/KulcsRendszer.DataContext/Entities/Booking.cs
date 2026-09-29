using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Entities {
    public class Booking {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }

        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();
    }
}