using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using KulcsRendszer.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class AuditLogServiceTests : IAsyncLifetime {
    private static readonly DateTimeOffset BaseTime =
        new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    private string _directory = null!;
    private AuditLogService _service = null!;

    private string LogFilePath => Path.Combine(_directory, "audit-log.jsonl");

    public async Task InitializeAsync() {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "kulcsrendszer-audit-tests",
            Guid.NewGuid().ToString());
        _service = CreateService(_directory, _directory);
        await _service.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync() {
        await _service.StopAsync(CancellationToken.None);
        if (Directory.Exists(_directory)) {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static AuditLogService CreateService(
        string? configuredDirectory,
        string contentRootPath) {
        var settings = new Dictionary<string, string?>();
        if (configuredDirectory != null) {
            settings["AuditLog:Directory"] = configuredDirectory;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new AuditLogService(
            configuration,
            new TestWebHostEnvironment(contentRootPath),
            NullLogger<AuditLogService>.Instance);
    }

    private static AuditLogEntry CreateEntry(
        int minuteOffset,
        string method = "GET",
        int statusCode = 200,
        string path = "/api/test") => new AuditLogEntry {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = BaseTime.AddMinutes(minuteOffset),
            UserId = "1",
            UserName = "Teszt Elek",
            Roles = "Admin",
            Method = method,
            Path = path,
            StatusCode = statusCode,
            UserAgent = "xunit",
            DurationMilliseconds = 5
        };

    private Task<AuditLogPage> GetAllAsync() =>
        _service.GetPageAsync(null, null, null, null, 1, 100);

    [Fact]
    public async Task GetPageAsync_ShouldReturnEmptyPage_WhenNothingWasLogged() {
        var result = await GetAllAsync();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task AppendAsync_ShouldPersistEntryWithAllFields() {
        var entry = CreateEntry(0, "POST", 201, "/api/bookings");

        await _service.AppendAsync(entry);
        var result = await GetAllAsync();

        var saved = Assert.Single(result.Items);
        Assert.Equal(entry.EventId, saved.EventId);
        Assert.Equal(entry.OccurredAtUtc, saved.OccurredAtUtc);
        Assert.Equal("1", saved.UserId);
        Assert.Equal("Teszt Elek", saved.UserName);
        Assert.Equal("Admin", saved.Roles);
        Assert.Equal("POST", saved.Method);
        Assert.Equal("/api/bookings", saved.Path);
        Assert.Equal(201, saved.StatusCode);
        Assert.Equal("xunit", saved.UserAgent);
        Assert.Equal(5, saved.DurationMilliseconds);
    }

    [Fact]
    public async Task AppendAsync_ShouldWriteOneJsonLinePerEntry() {
        await _service.AppendAsync(CreateEntry(0));
        await _service.AppendAsync(CreateEntry(1));

        var lines = await File.ReadAllLinesAsync(LogFilePath);

        Assert.Equal(2, lines.Length);
        Assert.All(
            lines,
            line => Assert.NotNull(JsonSerializer.Deserialize<AuditLogEntry>(
                line,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))));
    }

    [Fact]
    public async Task AppendAsync_ShouldUseAppDataFolder_WhenNoDirectoryIsConfigured() {
        var service = CreateService(null, _directory);
        await service.StartAsync(CancellationToken.None);
        try {
            await service.AppendAsync(CreateEntry(0));
        }
        finally {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.True(File.Exists(
            Path.Combine(_directory, "App_Data", "audit-log.jsonl")));
    }

    [Fact]
    public async Task GetPageAsync_ShouldReturnNewestEntryFirst() {
        var older = CreateEntry(0);
        var newer = CreateEntry(10);
        await _service.AppendAsync(older);
        await _service.AppendAsync(newer);

        var result = await GetAllAsync();

        Assert.Equal(
            new[] { newer.EventId, older.EventId },
            result.Items.Select(entry => entry.EventId));
    }

    [Fact]
    public async Task GetPageAsync_ShouldFilterByMethod_IgnoringCaseAndWhitespace() {
        await _service.AppendAsync(CreateEntry(0, "GET"));
        await _service.AppendAsync(CreateEntry(1, "POST"));

        var result = await _service.GetPageAsync(null, null, " post ", null, 1, 100);

        var entry = Assert.Single(result.Items);
        Assert.Equal("POST", entry.Method);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_ShouldFilterByStatusCode() {
        await _service.AppendAsync(CreateEntry(0, statusCode: 200));
        await _service.AppendAsync(CreateEntry(1, statusCode: 403));
        await _service.AppendAsync(CreateEntry(2, statusCode: 403));

        var result = await _service.GetPageAsync(null, null, null, 403, 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, entry => Assert.Equal(403, entry.StatusCode));
    }

    [Fact]
    public async Task GetPageAsync_ShouldIncludeFromAndExcludeTo() {
        var before = CreateEntry(-1);
        var atFrom = CreateEntry(0);
        var inside = CreateEntry(30);
        var atTo = CreateEntry(60);
        foreach (var entry in new[] { before, atFrom, inside, atTo }) {
            await _service.AppendAsync(entry);
        }

        var result = await _service.GetPageAsync(
            BaseTime,
            BaseTime.AddMinutes(60),
            null,
            null,
            1,
            100);

        Assert.Equal(
            new[] { inside.EventId, atFrom.EventId },
            result.Items.Select(entry => entry.EventId));
    }

    [Fact]
    public async Task GetPageAsync_ShouldReturnRequestedPage() {
        var entries = Enumerable.Range(0, 5).Select(i => CreateEntry(i)).ToList();
        foreach (var entry in entries) {
            await _service.AppendAsync(entry);
        }

        var result = await _service.GetPageAsync(null, null, null, null, 2, 2);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(
            new[] { entries[2].EventId, entries[1].EventId },
            result.Items.Select(entry => entry.EventId));
    }

    [Fact]
    public async Task GetPageAsync_ShouldReturnNoItems_WhenPageIsBeyondLastPage() {
        await _service.AppendAsync(CreateEntry(0));

        var result = await _service.GetPageAsync(null, null, null, null, 3, 10);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_ShouldSkipEmptyLines() {
        await _service.AppendAsync(CreateEntry(0));
        await File.AppendAllTextAsync(
            LogFilePath,
            Environment.NewLine + "   " + Environment.NewLine);
        await _service.AppendAsync(CreateEntry(1));

        var result = await GetAllAsync();

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_ShouldThrowInvalidDataException_WhenLineIsNotAnEntry() {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(LogFilePath, "null" + Environment.NewLine);

        await Assert.ThrowsAsync<InvalidDataException>(() => GetAllAsync());
    }

    [Fact]
    public async Task AppendAsync_ShouldKeepEveryEntry_WhenCalledConcurrently() {
        const int count = 200;
        var entries = Enumerable.Range(0, count).Select(i => CreateEntry(i)).ToList();

        await Task.WhenAll(
            entries.Select(entry => Task.Run(() => _service.AppendAsync(entry))));

        var lines = await File.ReadAllLinesAsync(LogFilePath);
        var result = await _service.GetPageAsync(null, null, null, null, 1, count);
        Assert.Equal(count, lines.Length);
        Assert.Equal(count, result.TotalCount);
        Assert.Equal(
            entries.Select(entry => entry.EventId).OrderBy(id => id),
            result.Items.Select(entry => entry.EventId).OrderBy(id => id));
    }

    [Fact]
    public async Task GetPageAsync_ShouldNotFail_WhenReadingDuringConcurrentWrites() {
        var writes = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => _service.AppendAsync(CreateEntry(i))));
        var reads = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => GetAllAsync()));

        await Task.WhenAll(writes.Concat<Task>(reads));

        var result = await GetAllAsync();
        Assert.Equal(100, result.TotalCount);
    }

    [Fact]
    public async Task AppendAsync_ShouldThrow_AfterServiceIsStopped() {
        await _service.StopAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ChannelClosedException>(
            () => _service.AppendAsync(CreateEntry(0)));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment {
        public TestWebHostEnvironment(string contentRootPath) {
            ContentRootPath = contentRootPath;
            WebRootPath = contentRootPath;
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "KulcsRendszer.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
    }
}
