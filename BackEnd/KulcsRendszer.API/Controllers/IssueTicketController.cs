using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using KulcsRendszer.DataContext.Dtos;
using KulcsRendszer.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Controllers {
    [ApiController]
    [Route("api/issuetickets")]
    [Authorize]
    public class IssueTicketController : ControllerBase {
        private readonly IIssueTicketService _issueTicketService;

        public IssueTicketController(IIssueTicketService issueTicketService) {
            _issueTicketService = issueTicketService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Portas")]
        [ProducesResponseType(
            typeof(IReadOnlyList<IssueTicketDto>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<IssueTicketDto>>> GetAll(
            CancellationToken cancellationToken) {
            var tickets = await _issueTicketService.GetAllAsync(cancellationToken);
            return Ok(tickets);
        }

        [HttpGet("mine")]
        [ProducesResponseType(
            typeof(IReadOnlyList<IssueTicketDto>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<IssueTicketDto>>> GetMine(
            CancellationToken cancellationToken) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var reporterId)) {
                return Unauthorized();
            }

            var tickets = await _issueTicketService.GetByReporterAsync(
                reporterId,
                cancellationToken);

            return Ok(tickets);
        }

        [HttpPost]
        [ProducesResponseType(typeof(IssueTicketDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<IssueTicketDto>> Create(
            [FromBody] IssueTicketCreateDto issueTicketCreateDto,
            CancellationToken cancellationToken) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var reporterId)) {
                return Unauthorized();
            }

            var ticket = await _issueTicketService.CreateAsync(
                issueTicketCreateDto,
                reporterId,
                cancellationToken);

            return Ok(ticket);
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin,Portas")]
        [ProducesResponseType(typeof(IssueTicketDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<IssueTicketDto>> UpdateStatus(
            int id,
            [FromBody] IssueTicketStatusUpdateDto statusUpdateDto,
            CancellationToken cancellationToken) {
            var ticket = await _issueTicketService.UpdateStatusAsync(
                id,
                statusUpdateDto.Status,
                cancellationToken);

            return Ok(ticket);
        }
    }
}
