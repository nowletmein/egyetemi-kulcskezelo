using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

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

    public class AuditLogService : IAuditLogService, IHostedService {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly string _filePath;
        private readonly ILogger<AuditLogService> _logger;
        private readonly Channel<QueuedAuditLogEntry> _writeQueue =
            Channel.CreateBounded<QueuedAuditLogEntry>(
                new BoundedChannelOptions(1024) {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private Task _writerTask = Task.CompletedTask;

        public AuditLogService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AuditLogService> logger) {
            _logger = logger;
            var configuredDirectory = configuration["AuditLog:Directory"];
            var directory = string.IsNullOrWhiteSpace(configuredDirectory)
                ? Path.Combine(environment.ContentRootPath, "App_Data")
                : Path.GetFullPath(configuredDirectory, environment.ContentRootPath);

            _filePath = Path.Combine(directory, "audit-log.jsonl");
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _writerTask = ProcessWriteQueueAsync();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken) {
            _writeQueue.Writer.TryComplete();
            await _writerTask.WaitAsync(cancellationToken);
        }

        public async Task AppendAsync(
            AuditLogEntry entry,
            CancellationToken cancellationToken = default) {
            var queuedEntry = new QueuedAuditLogEntry(
                JsonSerializer.Serialize(entry, JsonOptions));
            await _writeQueue.Writer.WriteAsync(queuedEntry, cancellationToken);
            await queuedEntry.Completion.Task;
        }

        public async Task<AuditLogPage> GetPageAsync(
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtcExclusive,
            string? method,
            int? statusCode,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
            var lines = new List<string>();
            await _fileLock.WaitAsync(cancellationToken);
            try {
                if (File.Exists(_filePath)) {
                    using var reader = new StreamReader(_filePath, Encoding.UTF8);
                    while (await reader.ReadLineAsync(cancellationToken) is { } line) {
                        if (!string.IsNullOrWhiteSpace(line)) {
                            lines.Add(line);
                        }
                    }
                }
            }
            finally {
                _fileLock.Release();
            }

            var matchingEntries = new ConcurrentBag<AuditLogEntry>();
            await Parallel.ForEachAsync(
                lines,
                new ParallelOptions {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount)
                },
                (line, _) => {
                    var entry = JsonSerializer.Deserialize<AuditLogEntry>(
                        line,
                        JsonOptions);
                    if (entry is null) {
                        throw new InvalidDataException(
                            "An audit log entry could not be deserialized.");
                    }

                    if ((!fromUtc.HasValue ||
                         entry.OccurredAtUtc >= fromUtc.Value) &&
                        (!toUtcExclusive.HasValue ||
                         entry.OccurredAtUtc < toUtcExclusive.Value) &&
                        (string.IsNullOrWhiteSpace(method) ||
                         string.Equals(
                             entry.Method,
                             method.Trim(),
                             StringComparison.OrdinalIgnoreCase)) &&
                        (!statusCode.HasValue ||
                         entry.StatusCode == statusCode.Value)) {
                        matchingEntries.Add(entry);
                    }

                    return ValueTask.CompletedTask;
                });

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

        private async Task ProcessWriteQueueAsync() {
            await foreach (var queuedEntry in _writeQueue.Reader.ReadAllAsync()) {
                try {
                    await AppendToFileAsync(queuedEntry.SerializedEntry);
                    queuedEntry.Completion.TrySetResult();
                }
                catch (Exception exception) {
                    queuedEntry.Completion.TrySetException(exception);
                    _logger.LogError(
                        exception,
                        "Could not persist an audit log entry.");
                }
            }
        }

        private async Task AppendToFileAsync(string serializedEntry) {
            await _fileLock.WaitAsync();
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
                await writer.WriteLineAsync(serializedEntry.AsMemory());
                await writer.FlushAsync();
            }
            finally {
                _fileLock.Release();
            }
        }

        private sealed class QueuedAuditLogEntry {
            public QueuedAuditLogEntry(string serializedEntry) {
                SerializedEntry = serializedEntry;
            }

            public string SerializedEntry { get; }
            public TaskCompletionSource Completion { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
