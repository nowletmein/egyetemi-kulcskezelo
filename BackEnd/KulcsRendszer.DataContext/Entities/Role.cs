using KulcsRendszer.DataContext.Enums;

namespace KulcsRendszer.DataContext.Entities {
    public class Role {
        public int Id { get; set; }
        public UserRole Name { get; set; }
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
