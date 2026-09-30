
using KulcsRendszer.DataContext.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;

namespace KulcsRendszer.DataContext.Context {
    public class KulcsRendszerDbContext : DbContext {

        public KulcsRendszerDbContext(DbContextOptions<KulcsRendszerDbContext> options) : base(options) { }
        
        //itt adom hozzá a User objectet Entity ként az adatbázishoz
        //      ||
        //      \/
        public DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Maintenance> Maintenances { get; set; }
        public DbSet<KeyMovement> KeyMovements { get; set; }
        public DbSet<Keys> Keys { get; set; }
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<IssueTicket> IssueTickets { get; set; }
        public DbSet<MasterKeyPermission> MasterKeyPermissions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder) { 

        }



    }
}