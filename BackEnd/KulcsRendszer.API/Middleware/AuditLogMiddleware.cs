using System.Diagnostics;
using System.Security.Claims;
using KulcsRendszer.API.Services;

namespace KulcsRendszer.API.Middleware {
    public class AuditLogMiddleware {
        private readonly RequestDelegate _next;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogMiddleware> _logger;

        public AuditLogMiddleware(
            RequestDelegate next,
            IAuditLogService auditLogService,
            ILogger<AuditLogMiddleware> logger) {
            _next = next;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) {
            if (HttpMethods.IsOptions(context.Request.Method)) {
                await _next(context);
                return;
            }

            var startedAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var userId = Limit(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                256);
            var userName = Limit(context.User.Identity?.Name, 256);
            var roles = Limit(
                string.Join(
                    ",",
                    context.User.FindAll(ClaimTypes.Role)
                        .Select(claim => claim.Value)
                        .Distinct(StringComparer.Ordinal)),
                512);
            var method = Limit(context.Request.Method, 10) ?? "UNKNOWN";
            var path = Limit(context.Request.Path.Value ?? "/", 2048) ?? "/";
            var userAgent = Limit(context.Request.Headers.UserAgent.ToString(), 512);
            var eventId = Guid.NewGuid();

            context.Response.OnCompleted(async () => {
                stopwatch.Stop();
                try {
                    await _auditLogService.AppendAsync(new AuditLogEntry {
                        EventId = eventId,
                        OccurredAtUtc = startedAt,
                        UserId = userId,
                        UserName = userName,
                        Roles = roles,
                        Method = method,
                        Path = path,
                        StatusCode = context.Response.StatusCode,
                        UserAgent = userAgent,
                        DurationMilliseconds = stopwatch.ElapsedMilliseconds
                    });
                }
                catch (Exception exception) {
                    _logger.LogError(
                        exception,
                        "Could not persist audit event {AuditEventId} for {Method} {Path}",
                        eventId,
                        method,
                        path);
                }
            });

            await _next(context);
        }

        private static string? Limit(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength
                ? value
                : value[..maxLength];
    }
}
