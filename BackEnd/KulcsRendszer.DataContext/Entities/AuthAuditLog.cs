namespace KulcsRendszer.DataContext.Entities {
    public class AuthAuditLog {
        public int Id { get; set; }
        public string EventType { get; set; } = "";
        public string EmailIdentifier { get; set; } = "";
        public string? IpAddress { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
