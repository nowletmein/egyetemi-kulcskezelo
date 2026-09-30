using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KulcsRendszer.DataContext.Entities {
    public class MasterKeyPermission {

        public int Id { get; set; }
        public int KeysId { get; set; }
        public int UserId { get; set; }
        public int AdminId { get; set; }
        public DateTime Date { get; set; }

        public Keys Keys { get; set; } = null!;
        public User User { get; set; } = null!;
        public User Admin { get; set; } = null!;
    }
}
