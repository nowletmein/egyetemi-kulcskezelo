using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Entities {
    public class Keys {

        public int Id { get; set; }
        public string RoomId { get; set; }
        public string Type { get; set; }

        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();
        public ICollection<IssueTicket> IssueTickets { get; set; } = new List<IssueTicket>();
        public ICollection<MasterKeyPermission> MasterKeyPermissions { get; set; } = new List<MasterKeyPermission>();
    }
}