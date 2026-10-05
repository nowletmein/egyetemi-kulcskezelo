using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using Microsoft.EntityFrameworkCore;

namespace KulcsRendszer.DataContext.Context {
    public class KulcsRendszerDbContext : DbContext {
        public KulcsRendszerDbContext(DbContextOptions<KulcsRendszerDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Maintenance> Maintenances { get; set; }
        public DbSet<KeyMovement> KeyMovements { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<MasterKey> MasterKeys { get; set; }
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<IssueTicket> IssueTickets { get; set; }
        public DbSet<MasterKeyPermission> MasterKeyPermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // Table-per-Hierarchy (TPH) for Key and MasterKey
            modelBuilder.Entity<Key>()
                .HasDiscriminator<string>("KeyType")
                .HasValue<Key>("Standard")
                .HasValue<MasterKey>("Master");

            // Standard Key -> ClassRoom (One-to-Many)
            modelBuilder.Entity<Key>()
                .HasOne(k => k.ClassRoom)
                .WithMany(r => r.Keys)
                .HasForeignKey(k => k.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // MasterKey -> AccessibleRooms (Many-to-Many join table)
            modelBuilder.Entity<MasterKey>()
                .HasMany(m => m.AccessibleRooms)
                .WithMany();

            // MasterKeyPermission -> User (Recipient)
            modelBuilder.Entity<MasterKeyPermission>()
                .HasOne(p => p.User)
                .WithMany(u => u.MasterKeyPermissions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // MasterKeyPermission -> User (Admin Granter)
            modelBuilder.Entity<MasterKeyPermission>()
                .HasOne(p => p.GrantedByAdmin)
                .WithMany(u => u.GrantedPermissions)
                .HasForeignKey(p => p.GrantedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // MasterKeyPermission -> Key
            modelBuilder.Entity<MasterKeyPermission>()
                .HasOne(p => p.Key)
                .WithMany(k => k.MasterKeyPermissions)
                .HasForeignKey(p => p.KeyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking -> ClassRoom
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Maintenance -> ClassRoom
            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.Room)
                .WithMany(r => r.Maintenances)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Maintenance -> Admin User
            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.Admin)
                .WithMany(u => u.OrderedMaintenances)
                .HasForeignKey(m => m.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // IssueTicket -> Reporter User
            modelBuilder.Entity<IssueTicket>()
                .HasOne(t => t.Reporter)
                .WithMany(u => u.ReportedTickets)
                .HasForeignKey(t => t.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // IssueTicket -> ClassRoom
            modelBuilder.Entity<IssueTicket>()
                .HasOne(t => t.Room)
                .WithMany(r => r.IssueTickets)
                .HasForeignKey(t => t.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // IssueTicket -> Key
            modelBuilder.Entity<IssueTicket>()
                .HasOne(t => t.Key)
                .WithMany(k => k.IssueTickets)
                .HasForeignKey(t => t.KeyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Store UserRole enum as string in database
            modelBuilder.Entity<Role>()
                .Property(r => r.Name)
                .HasConversion<string>();

            // Relationship between User and Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed default roles using the UserRole enum
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = UserRole.Admin },
                new Role { Id = 2, Name = UserRole.Portas },
                new Role { Id = 3, Name = UserRole.Igazgato },
                new Role { Id = 4, Name = UserRole.Oktato }
            );
        }
    }
}
