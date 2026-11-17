using System;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.API.Controllers;
using KulcsRendszer.API.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public class AuditLogControllerTests {
    private readonly FakeAuditLogService _service = new();
    private readonly AuditLogController _controller;

    public AuditLogControllerTests() {
        _controller = new AuditLogController(_service);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetAuditLog_ShouldReturnBadRequest_WhenPagingIsInvalid(
        int page,
        int pageSize) {
        var result = await _controller.GetAuditLog(null, null, null, null, page, pageSize);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, _service.Calls);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnBadRequest_WhenFromIsNotBeforeTo() {
        var time = new DateTimeOffset(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

        var result = await _controller.GetAuditLog(time, time, null, null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, _service.Calls);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public async Task GetAuditLog_ShouldReturnBadRequest_WhenStatusCodeIsOutOfRange(
        int statusCode) {
        var result = await _controller.GetAuditLog(null, null, null, statusCode);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, _service.Calls);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnBadRequest_WhenPageIsTooLarge() {
        var result = await _controller.GetAuditLog(null, null, null, null, int.MaxValue, 100);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, _service.Calls);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnOkAndPassFilters_WhenRequestIsValid() {
        var from = new DateTimeOffset(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);
        var to = from.AddHours(2);

        var result = await _controller.GetAuditLog(from, to, "POST", 201, 2, 25);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<AuditLogPage>(okResult.Value);
        Assert.Equal(2, page.Page);
        Assert.Equal(25, page.PageSize);
        Assert.Equal(1, _service.Calls);
        Assert.Equal(from, _service.LastQuery.From);
        Assert.Equal(to, _service.LastQuery.To);
        Assert.Equal("POST", _service.LastQuery.Method);
        Assert.Equal(201, _service.LastQuery.StatusCode);
        Assert.Equal(2, _service.LastQuery.Page);
        Assert.Equal(25, _service.LastQuery.PageSize);
    }

    private sealed class FakeAuditLogService : IAuditLogService {
        public int Calls { get; private set; }

        public (DateTimeOffset? From, DateTimeOffset? To, string? Method,
            int? StatusCode, int Page, int PageSize) LastQuery { get; private set; }

        public Task AppendAsync(
            AuditLogEntry entry,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AuditLogPage> GetPageAsync(
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtcExclusive,
            string? method,
            int? statusCode,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
            Calls++;
            LastQuery = (fromUtc, toUtcExclusive, method, statusCode, page, pageSize);
            return Task.FromResult(new AuditLogPage {
                Page = page,
                PageSize = pageSize
            });
        }
    }
}
