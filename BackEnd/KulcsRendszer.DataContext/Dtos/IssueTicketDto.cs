namespace KulcsRendszer.DataContext.Dtos {
    public class IssueTicketCreateDto {
        public int? KeyId { get; set; }
        public string RoomId { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class IssueTicketStatusUpdateDto {
        public string Status { get; set; } = "";
    }

    public class IssueTicketDto {
        public int Id { get; set; }
        public int? KeyId { get; set; }
        public string RoomId { get; set; } = "";
        public int ReporterId { get; set; }
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
