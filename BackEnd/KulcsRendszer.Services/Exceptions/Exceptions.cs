using System;
using System.Text;
using System.Threading.Tasks;

namespace KulcsRendszer.Services.Exceptions {

    public interface INotFoundException { }
    public interface IAlreadyExistsException { }

    public class UserNotFoundException : Exception, INotFoundException {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class UserAlreadyExistsException : Exception, IAlreadyExistsException {
        public UserAlreadyExistsException(string message) : base(message) { }
    }

    public class RoomNotFoundException : Exception, INotFoundException {
        public RoomNotFoundException(string message) : base(message) { }
    }

    public class RoomAlreadyExistsException : Exception, IAlreadyExistsException {
        public RoomAlreadyExistsException(string message) : base(message) { }
    }

    public class ClassRoomNotFoundException : Exception, INotFoundException {
        public ClassRoomNotFoundException(string message) : base(message) { }
    }

    public class ClassRoomAlreadyExistsException : Exception, IAlreadyExistsException {
        public ClassRoomAlreadyExistsException(string message) : base(message) { }
    }

    public class BookingNotFoundException : Exception, INotFoundException {
        public BookingNotFoundException(string message) : base(message) { }
    }

    public class BookingAlreadyExistsException : Exception, IAlreadyExistsException {
        public BookingAlreadyExistsException(string message) : base(message) { }
    }

    public class BuildingNotFoundException : Exception, INotFoundException {
        public BuildingNotFoundException(string message) : base(message) { }
    }

    public class BuildingAlreadyExistsException : Exception, IAlreadyExistsException {
        public BuildingAlreadyExistsException(string message) : base(message) { }
    }

    public class FloorNotFoundException : Exception, INotFoundException {
        public FloorNotFoundException(string message) : base(message) { }
    }

    public class FloorAlreadyExistsException : Exception, IAlreadyExistsException {
        public FloorAlreadyExistsException(string message) : base(message) { }
    }

    public class IssueTicketNotFoundException : Exception, INotFoundException {
        public IssueTicketNotFoundException(string message) : base(message) { }
    }

    public class IssueTicketAlreadyExistsException : Exception, IAlreadyExistsException {
        public IssueTicketAlreadyExistsException(string message) : base(message) { }
    }

    public class KeysNotFoundException : Exception, INotFoundException {
        public KeysNotFoundException(string message) : base(message) { }
    }

    public class KeysAlreadyExistsException : Exception, IAlreadyExistsException {
        public KeysAlreadyExistsException(string message) : base(message) { }
    }

    public class RoleNotFoundException : Exception, INotFoundException {
        public RoleNotFoundException(string message) : base(message) { }
    }

    public class RoleAlreadyExistsException : Exception, IAlreadyExistsException {
        public RoleAlreadyExistsException(string message) : base(message) { }
    }

    public class ScreeningNotFoundException : Exception, INotFoundException {
        public ScreeningNotFoundException(string message) : base(message) { }
    }

    public class ScreeningAlreadyExistsException : Exception, IAlreadyExistsException {
        public ScreeningAlreadyExistsException(string message) : base(message) { }
    }
}
