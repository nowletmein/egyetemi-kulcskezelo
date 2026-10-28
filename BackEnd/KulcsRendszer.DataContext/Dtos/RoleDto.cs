using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Enums;

namespace KulcsRendszer.DataContext.Dtos {
    public class RoleDto {
        public int Id { get; set; }
        public UserRole Name { get; set; }
    }
    public class RoleCreateDto {
        public UserRole Name { get; set; }
    }
}
