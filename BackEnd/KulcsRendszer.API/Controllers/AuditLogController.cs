using KulcsRendszer.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/audit-log")]
    [Authorize(Roles = "Igazgato")]
    public class AuditLogController : ControllerBase {
        private readonly AuditLogStore _auditLogStore;

        public AuditLogController(AuditLogStore auditLogStore) {
            _auditLogStore = auditLogStore;
        }

        [HttpGet]
        [ProducesResponseType(typeof(AuditLogPage), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuditLogPage>> GetAuditLog(
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtcExclusive,
            [FromQuery] string? method,
            [FromQuery] int? statusCode,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default) {
            if (page < 1 || pageSize < 1 || pageSize > 100) {
                return BadRequest(new {
                    message = "A page legalább 1, a pageSize 1 és 100 közötti lehet."
                });
            }
            if (fromUtc.HasValue &&
                toUtcExclusive.HasValue &&
                fromUtc >= toUtcExclusive) {
                return BadRequest(new {
                    message = "A kezdő időpontnak korábbinak kell lennie a záró időpontnál."
                });
            }
            if (statusCode.HasValue &&
                (statusCode.Value < 100 || statusCode.Value > 599)) {
                return BadRequest(new {
                    message = "A HTTP státuszkódnak 100 és 599 közé kell esnie."
                });
            }
            if (((long)page - 1) * pageSize > int.MaxValue) {
                return BadRequest(new { message = "A kért oldal túl nagy." });
            }

            var result = await _auditLogStore.GetPageAsync(
                fromUtc,
                toUtcExclusive,
                method,
                statusCode,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
    }
}
