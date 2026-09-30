using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Entities {
    public class ClassRoom {

        public string Id { get; set; } = "";
        public string Building { get; set; } = "";
        public int Floor { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
        public ICollection<IssueTicket> IssueTickets { get; set; } = new List<IssueTicket>();
    }
}
