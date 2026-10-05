namespace KulcsRendszer.DataContext.Entities {
    public class MasterKeyPermission {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int KeyId { get; set; }
        public Key Key { get; set; } = null!;

        public int GrantedByAdminId { get; set; }
        public User GrantedByAdmin { get; set; } = null!;

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}