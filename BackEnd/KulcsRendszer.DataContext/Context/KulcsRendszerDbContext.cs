
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
        
        protected override void OnModelCreating(ModelBuilder modelBuilder) { 

        }



    }
}