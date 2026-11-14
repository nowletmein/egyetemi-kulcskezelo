using System.Diagnostics;
using System.Security.Claims;
using KulcsRendszer.API.Services;

namespace KulcsRendszer.API.Middleware {
    public class AuditLogMiddleware {
        private readonly RequestDelegate _next;
        private readonly AuditLogStore _auditLogStore;
        private readonly ILogger<AuditLogMiddleware> _logger;

        public AuditLogMiddleware(
            RequestDelegate next,
            AuditLogStore auditLogStore,
            ILogger<AuditLogMiddleware> logger) {
            _next = next;
            _auditLogStore = auditLogStore;
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
                    await _auditLogStore.AppendAsync(new AuditLogEntry(
                        eventId,
                        startedAt,
                        userId,
                        userName,
                        roles,
                        method,
                        path,
                        context.Response.StatusCode,
                        userAgent,
                        stopwatch.ElapsedMilliseconds));
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
