using System.Text;
using System.Text.Json;

namespace KulcsRendszer.API.Services {
    public class AuditLogEntry {
        public Guid EventId { get; set; }
        public DateTimeOffset OccurredAtUtc { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Roles { get; set; }
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public int StatusCode { get; set; }
        public string? UserAgent { get; set; }
        public long DurationMilliseconds { get; set; }
    }

    public class AuditLogPage {
        public IReadOnlyList<AuditLogEntry> Items { get; set; } = Array.Empty<AuditLogEntry>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public interface IAuditLogService {
        Task AppendAsync(
            AuditLogEntry entry,
            CancellationToken cancellationToken = default);

        Task<AuditLogPage> GetPageAsync(
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtcExclusive,
            string? method,
            int? statusCode,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }

    public class AuditLogService : IAuditLogService {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly string _filePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        public AuditLogService(
            IConfiguration configuration,
            IWebHostEnvironment environment) {
            var configuredDirectory = configuration["AuditLog:Directory"];
            var directory = string.IsNullOrWhiteSpace(configuredDirectory)
                ? Path.Combine(environment.ContentRootPath, "App_Data")
                : Path.GetFullPath(configuredDirectory, environment.ContentRootPath);

            _filePath = Path.Combine(directory, "audit-log.jsonl");
        }

        public async Task AppendAsync(
            AuditLogEntry entry,
            CancellationToken cancellationToken = default) {
            await _fileLock.WaitAsync(cancellationToken);
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                await using var stream = new FileStream(
                    _filePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);
                await using var writer = new StreamWriter(
                    stream,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                await writer.WriteLineAsync(
                    JsonSerializer.Serialize(entry, JsonOptions).AsMemory(),
                    cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
            finally {
                _fileLock.Release();
            }
        }

        public async Task<AuditLogPage> GetPageAsync(
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtcExclusive,
            string? method,
            int? statusCode,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
            await _fileLock.WaitAsync(cancellationToken);
            try {
                var matchingEntries = new List<AuditLogEntry>();
                if (File.Exists(_filePath)) {
                    using var reader = new StreamReader(_filePath, Encoding.UTF8);
                    while (await reader.ReadLineAsync(cancellationToken) is { } line) {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var entry = JsonSerializer.Deserialize<AuditLogEntry>(
                            line,
                            JsonOptions);
                        if (entry is null) {
                            throw new InvalidDataException(
                                "An audit log entry could not be deserialized.");
                        }

                        if (fromUtc.HasValue &&
                            entry.OccurredAtUtc < fromUtc.Value) {
                            continue;
                        }
                        if (toUtcExclusive.HasValue &&
                            entry.OccurredAtUtc >= toUtcExclusive.Value) {
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(method) &&
                            !string.Equals(
                                entry.Method,
                                method.Trim(),
                                StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }
                        if (statusCode.HasValue &&
                            entry.StatusCode != statusCode.Value) {
                            continue;
                        }

                        matchingEntries.Add(entry);
                    }
                }

                var totalCount = matchingEntries.Count;
                var items = matchingEntries
                    .OrderByDescending(entry => entry.OccurredAtUtc)
                    .ThenByDescending(entry => entry.EventId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToArray();

                return new AuditLogPage {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            finally {
                _fileLock.Release();
            }
        }
    }
}
