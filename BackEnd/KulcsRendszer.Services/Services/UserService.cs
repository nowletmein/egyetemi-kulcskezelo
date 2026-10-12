using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
namespace KulcsRendszer.Services.Services {

    public interface IUserService {
        //NOTE: make sure you add every public function to the interface too like bellow --Ben
        Task<UserDto> Get(int userId);
    }
    public class UserService : IUserService {
        private readonly KulcsRendszerDbContext _context;
        public UserService(KulcsRendszerDbContext context) {
            _context = context;
        }

        //function that maps User object to UserDto
        private UserDto MapToUserDto(User user) {
            throw new NotImplementedException();
        }

        //Gets User object by Id, converts it to UserDto
        public async Task<UserDto> Get(int userId) {
            throw new NotImplementedException();
        }
    }
}

