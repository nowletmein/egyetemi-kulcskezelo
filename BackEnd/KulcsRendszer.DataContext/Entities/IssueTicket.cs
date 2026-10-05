namespace KulcsRendszer.DataContext.Entities {
    public class IssueTicket {
        public int Id { get; set; }
        
        public int? KeyId { get; set; }
        public Key? Key { get; set; }
        
        public string RoomId { get; set; } = "";
        public ClassRoom Room { get; set; } = null!;
        
        public int ReporterId { get; set; }
        public User Reporter { get; set; } = null!;

        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
    }
}