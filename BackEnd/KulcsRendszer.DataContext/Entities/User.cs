using KulcsRendszer.DataContext.Enums;

namespace KulcsRendszer.DataContext.Entities {
    public class User {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();
        public ICollection<IssueTicket> ReportedTickets { get; set; } = new List<IssueTicket>();
        public ICollection<Maintenance> OrderedMaintenances { get; set; } = new List<Maintenance>();

        public ICollection<MasterKeyPermission> MasterKeyPermissions { get; set; } = new List<MasterKeyPermission>();
        public ICollection<MasterKeyPermission> GrantedPermissions { get; set; } = new List<MasterKeyPermission>();
    }
}
