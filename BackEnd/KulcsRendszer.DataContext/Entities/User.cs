using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Enums;

namespace KulcsRendszer.DataContext.Entities {
    public class User {

        //attributumok (táblák) User(Id,Role,Name,Email,PasswordHash)
        public int Id { get; set; }
        public UserRole Role { get; set; }
        public string Name { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; }

        //Felhasználóhoz kötődő foglalások, Kulcs mozgatások, Hibajegyek, elrendelt karbantartások, Mesterkulcs jogok
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>(); 
        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();

        public ICollection<IssueTicket> ReportedTickets { get; set; } = new List<IssueTicket>();

        public ICollection<Maintenance> OrderedMaintenances { get; set; } = new List<Maintenance>();

        public ICollection<MasterKeyPermission> MasterKeyPermissions { get; set; } = new List<MasterKeyPermission>();

    }
}
