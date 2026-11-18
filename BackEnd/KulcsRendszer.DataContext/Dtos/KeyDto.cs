using KulcsRendszer.DataContext.Entities;

namespace KulcsRendszer.DataContext.Dtos {
    public class KeyDto {
        public int Id { get; set; }
        public string? RoomId { get; set; }
        public bool IsMasterKey { get; set; }
        public string MasterKeyName { get; set; } = "";

        public ICollection<string> AccessibleRoomIds { get; set; } = new List<string>();

        public ICollection<MasterKeyPermission> MasterKeyPermissions { get; set; } = new List<MasterKeyPermission>();
        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();
        public ICollection<IssueTicket> IssueTickets { get; set; } = new List<IssueTicket>();
    }

    public class KeyReturnDto { 
        public int UserId { get; set; }
        public int KeyId { get; set; }
        public int BookingId { get; set; }
    }
    public class KeyCreateDto {
        public string RoomId { get; set; } = string.Empty;
    }

    public class MasterKeyCreateDto {
        public string MasterKeyName { get; set; } = string.Empty;
        public ICollection<string> AccessibleRoomIds { get; set; } = new List<string>();
    }
}
