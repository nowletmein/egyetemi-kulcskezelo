using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;

namespace KulcsRendszer.DataContext.Dtos {
    public class UserDto {
        public int Id { get; set; }
        public int? RoleId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<KeyMovement> KeyMovements { get; set; } = new List<KeyMovement>();
        public ICollection<IssueTicket> ReportedTickets { get; set; } = new List<IssueTicket>();
        public ICollection<Maintenance> OrderedMaintenances { get; set; } = new List<Maintenance>();
    }

    public class UserCreateDto {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class UserUpdateDto {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class UserInfoDto {
        public int Id { get; set; }
        public int? RoleId { get; set; }
        public Role? Role { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class ChangePasswordDto {
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }

}
