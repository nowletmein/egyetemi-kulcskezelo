
namespace KulcsRendszer.DataContext.Entities {
    public class ClassRoom {

        public string Id { get; set; } = "";
        public string Building { get; set; } = "";
        public int FloorId { get; set; }
        public Floor Floor { get; set; } = null!;
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string Equipment { get; set; } = "";

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
        public ICollection<IssueTicket> IssueTickets { get; set; } = new List<IssueTicket>();
        public ICollection<Key> Keys { get; set; } = new List<Key>();
    }
}

