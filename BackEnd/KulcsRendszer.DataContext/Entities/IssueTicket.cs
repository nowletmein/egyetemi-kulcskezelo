using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.DataContext.Entities {
    public class IssueTicket {

        public int Id { get; set; }
        public int KeysId { get; set; }
        public string RoomId { get; set; } = "";
        public int ReporterId { get; set; }
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";

        public Keys Keys { get; set; } = null!;
        public User Reporter { get; set; } = null!;
    }
}
