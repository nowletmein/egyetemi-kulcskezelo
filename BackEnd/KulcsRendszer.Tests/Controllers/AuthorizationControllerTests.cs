using System.Security.Claims;
using KulcsRendszer.API.Controllers;
using KulcsRendszer.Controllers;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class BookingControllerAuthorizationTests {
    private readonly FakeBookingService _service = new();
    private readonly BookingController _controller;

    public BookingControllerAuthorizationTests() {
        _controller = new BookingController(_service);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-integer")]
    [InlineData("2147483648")]
    public async Task GetUserBookings_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid(
        string? userId) {
        SetUser(userId);

        var result = await _controller.GetUserBookings(7, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Equal(0, _service.GetUserBookingsCalls);
    }

    [Fact]
    public async Task GetUserBookings_ShouldForbidAccessToAnotherUsersBookings() {
        SetUser("7", "Oktato");

        var result = await _controller.GetUserBookings(8, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal(0, _service.GetUserBookingsCalls);
    }

    [Theory]
    [InlineData("Oktato", 7)]
    [InlineData("Admin", 8)]
    public async Task GetUserBookings_ShouldAllowOwnerOrAdmin(
        string role,
        int requestedUserId) {
        SetUser("7", role);

        var result = await _controller.GetUserBookings(
            requestedUserId,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, _service.GetUserBookingsCalls);
        Assert.Equal(requestedUserId, _service.LastRequestedUserId);
    }

    [Fact]
    public async Task AddUserBooking_ShouldForbidRoleOtherThanOktatoOrRoleless() {
        SetUser("7", "Portas");

        var result = await _controller.AddUserBooking(
            new BookingCreateDto { UserId = 7 },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal(0, _service.AddBookingCalls);
    }

    [Fact]
    public async Task AddUserBooking_ShouldForbidBookingForAnotherUser() {
        SetUser("7", "Oktato");

        var result = await _controller.AddUserBooking(
            new BookingCreateDto { UserId = 8 },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal(0, _service.AddBookingCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Oktato")]
    public async Task AddUserBooking_ShouldAllowRolelessUserOrOktatoToBookForSelf(
        string? role) {
        SetUser("7", role);

        var result = await _controller.AddUserBooking(
            new BookingCreateDto { UserId = 7 },
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status201Created,
            Assert.IsType<ObjectResult>(result.Result).StatusCode);
        Assert.Equal(1, _service.AddBookingCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task AddUserBooking_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid(
        string? userId) {
        SetUser(userId, "Oktato");

        var result = await _controller.AddUserBooking(
            new BookingCreateDto { UserId = 7 },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Equal(0, _service.AddBookingCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task DeleteUserBooking_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid(
        string? userId) {
        SetUser(userId);

        var result = await _controller.DeleteUserBooking(10, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(0, _service.GetBookingOwnerCalls);
        Assert.Equal(0, _service.DeleteBookingCalls);
    }

    [Fact]
    public async Task DeleteUserBooking_ShouldReturnNotFound_WhenBookingDoesNotExist() {
        SetUser("7");
        _service.BookingOwnerId = null;

        var result = await _controller.DeleteUserBooking(10, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, _service.DeleteBookingCalls);
    }

    [Fact]
    public async Task DeleteUserBooking_ShouldForbidNonOwner() {
        SetUser("7");
        _service.BookingOwnerId = 8;

        var result = await _controller.DeleteUserBooking(10, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, _service.DeleteBookingCalls);
    }

    [Theory]
    [InlineData("7", "Oktato", 7)]
    [InlineData("7", "Admin", 8)]
    public async Task DeleteUserBooking_ShouldAllowOwnerOrAdmin(
        string userId,
        string role,
        int bookingOwnerId) {
        SetUser(userId, role);
        _service.BookingOwnerId = bookingOwnerId;

        var result = await _controller.DeleteUserBooking(10, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(1, _service.DeleteBookingCalls);
    }

    private void SetUser(string? userId, string? role = null) {
        var claims = new List<Claim>();
        if (userId is not null) {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        if (role is not null) {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
    }

    private sealed class FakeBookingService : IBookingService {
        public int GetUserBookingsCalls { get; private set; }
        public int? LastRequestedUserId { get; private set; }
        public int GetBookingOwnerCalls { get; private set; }
        public int DeleteBookingCalls { get; private set; }
        public int AddBookingCalls { get; private set; }
        public int? BookingOwnerId { get; set; } = 8;

        public Task<IReadOnlyList<BookingDto>> GetAllBookingsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BookingDto>>(Array.Empty<BookingDto>());

        public Task<IReadOnlyList<BookingDto>> GetUserBookingsAsync(
            int userId,
            CancellationToken cancellationToken) {
            GetUserBookingsCalls++;
            LastRequestedUserId = userId;
            return Task.FromResult<IReadOnlyList<BookingDto>>(Array.Empty<BookingDto>());
        }

        public Task<BookingDto> AddUserBookingAsync(
            BookingCreateDto bookingCreateDto,
            CancellationToken cancellationToken) {
            AddBookingCalls++;
            return Task.FromResult(new BookingDto());
        }

        public Task<bool> DeleteUserBookingAsync(
            int bookingId,
            CancellationToken cancellationToken) {
            DeleteBookingCalls++;
            return Task.FromResult(true);
        }

        public Task<int?> GetBookingOwnerIdAsync(
            int bookingId,
            CancellationToken cancellationToken) {
            GetBookingOwnerCalls++;
            return Task.FromResult(BookingOwnerId);
        }
    }
}

public class KeyControllerAuthorizationTests {
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task GrantMasterKeyPermission_ShouldReturnUnauthorized_WhenAdminIdClaimIsInvalid(
        string? adminId) {
        var service = new FakeKeyService();
        var controller = new KeyController(service);
        var claims = adminId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, adminId) };
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };

        var result = await controller.GrantMasterKeyPermission(1, 2);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(0, service.GrantCalls);
    }

    [Fact]
    public async Task GrantMasterKeyPermission_ShouldPassAuthenticatedAdminIdToService() {
        var service = new FakeKeyService();
        var controller = new KeyController(service);
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "12") },
                    "test"))
            }
        };

        var result = await controller.GrantMasterKeyPermission(1, 2);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(1, service.GrantCalls);
        Assert.Equal((1, 2, 12), service.GrantRequest);
    }

    private sealed class FakeKeyService : IKeyService {
        public int GrantCalls { get; private set; }
        public (int KeyId, int UserId, int AdminId) GrantRequest { get; private set; }

        public Task<List<KeyDto>> GetAll() => Task.FromResult(new List<KeyDto>());
        public Task<KeyDto> Get(int keyId) => Task.FromResult(new KeyDto());
        public Task<KeyDto> Create(KeyDto keyDto) => Task.FromResult(new KeyDto());
        public Task<KeyDto> Update(int keyId, KeyDto keyDto) => Task.FromResult(new KeyDto());
        public Task<bool> Delete(int keyId) => Task.FromResult(true);

        public Task<MasterKeyPermission> GrantMasterKeyPermission(
            int keyId,
            int userId,
            int grantedByAdminId) {
            GrantCalls++;
            GrantRequest = (keyId, userId, grantedByAdminId);
            return Task.FromResult(new MasterKeyPermission());
        }

        public Task<bool> RevokeMasterKeyPermission(int keyId, int userId) =>
            Task.FromResult(true);
    }
}

public class IssueTicketControllerAuthorizationTests {
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("2147483648")]
    public async Task GetMine_ShouldReturnUnauthorized_WhenReporterClaimIsInvalid(
        string? reporterId) {
        var service = new FakeIssueTicketService();
        var controller = CreateController(service, reporterId);

        var result = await controller.GetMine(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Equal(0, service.GetByReporterCalls);
    }

    [Fact]
    public async Task GetMine_ShouldQueryOnlyTheReporterFromTheClaim() {
        var service = new FakeIssueTicketService();
        var controller = CreateController(service, "23");

        var result = await controller.GetMine(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, service.GetByReporterCalls);
        Assert.Equal(23, service.LastReporterId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task Create_ShouldReturnUnauthorized_WhenReporterClaimIsInvalid(
        string? reporterId) {
        var service = new FakeIssueTicketService();
        var controller = CreateController(service, reporterId);

        var result = await controller.Create(
            new IssueTicketCreateDto { RoomId = "A101", Description = "Fault" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Equal(0, service.CreateCalls);
    }

    [Fact]
    public async Task Create_ShouldUseReporterFromClaim() {
        var service = new FakeIssueTicketService();
        var controller = CreateController(service, "23");
        var dto = new IssueTicketCreateDto {
            RoomId = "A101",
            Description = "Fault"
        };

        var result = await controller.Create(dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, service.CreateCalls);
        Assert.Equal(23, service.LastReporterId);
        Assert.Same(dto, service.LastCreateDto);
    }

    private static IssueTicketController CreateController(
        FakeIssueTicketService service,
        string? reporterId) {
        var controller = new IssueTicketController(service);
        var claims = reporterId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, reporterId) };
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
        return controller;
    }

    private sealed class FakeIssueTicketService : IIssueTicketService {
        public int GetByReporterCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public int? LastReporterId { get; private set; }
        public IssueTicketCreateDto? LastCreateDto { get; private set; }

        public Task<IReadOnlyList<IssueTicketDto>> GetAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IssueTicketDto>>(
                Array.Empty<IssueTicketDto>());

        public Task<IReadOnlyList<IssueTicketDto>> GetByReporterAsync(
            int reporterId,
            CancellationToken cancellationToken) {
            GetByReporterCalls++;
            LastReporterId = reporterId;
            return Task.FromResult<IReadOnlyList<IssueTicketDto>>(
                Array.Empty<IssueTicketDto>());
        }

        public Task<IssueTicketDto> CreateAsync(
            IssueTicketCreateDto issueTicketCreateDto,
            int reporterId,
            CancellationToken cancellationToken) {
            CreateCalls++;
            LastReporterId = reporterId;
            LastCreateDto = issueTicketCreateDto;
            return Task.FromResult(new IssueTicketDto { ReporterId = reporterId });
        }

        public Task<IssueTicketDto> UpdateStatusAsync(
            int ticketId,
            string status,
            CancellationToken cancellationToken) =>
            Task.FromResult(new IssueTicketDto { Id = ticketId, Status = status });
    }
}
