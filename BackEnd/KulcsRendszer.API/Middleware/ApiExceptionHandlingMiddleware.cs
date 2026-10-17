using Microsoft.AspNetCore.Mvc;

namespace KulcsRendszer.API.Middleware {
    public sealed class ApiExceptionHandlingMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

        public ApiExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ApiExceptionHandlingMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            }
            catch (InvalidFloorIdException exception) {
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Invalid floor ID.",
                    exception.Message);
            }
            catch (FloorNotFoundException exception) {
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    "Floor not found.",
                    exception.Message);
            }
            catch (RoomsNotFoundException exception) {
                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    "Rooms not found.",
                    exception.Message);
            }
            catch (Exception exception) {
                _logger.LogError(exception, "An unhandled exception occurred.");

                await WriteProblemDetailsAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    "The server could not complete the request.");
            }
        }

        private static async Task WriteProblemDetailsAsync(
            HttpContext context,
            int statusCode,
            string title,
            string detail) {
            if (context.Response.HasStarted) {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            }, context.RequestAborted);
        }
    }

    public sealed class InvalidFloorIdException : Exception {
        public InvalidFloorIdException(string message) : base(message) {
        }
    }

    public sealed class FloorNotFoundException : Exception {
        public FloorNotFoundException(string message) : base(message) {
        }
    }

    public sealed class RoomsNotFoundException : Exception {
        public RoomsNotFoundException(string message) : base(message) {
        }
    }
}
