namespace KulcsRendszer.DataContext.Entities {
    public class Key {
        public int Id { get; set; }

        public string? RoomId { get; set; }
        public ClassRoom? ClassRoom { get; set; }

        public ICollection<MasterKeyPermission> MasterKeyPermissions { get; set; } = new List<MasterKeyPermission>();
        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();
        public ICollection<IssueTicket> IssueTickets { get; set; } = new List<IssueTicket>();
    }

    public class MasterKey : Key {
        public string MasterKeyName { get; set; } = "";
        public ICollection<ClassRoom> AccessibleRooms { get; set; } = new List<ClassRoom>();
    }
}