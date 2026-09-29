using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Entities {
    public class Maintenance {
        public int Id { get; set; }
        public string RoomId { get; set; }
        public int AdminId { get; set; }
        public User Admin { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}