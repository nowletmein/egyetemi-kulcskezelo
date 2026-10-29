using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.Services.Exceptions;

namespace KulcsRendszer.API.Middleware {

    public class ApiExceptionHandlingMiddleware {
        private readonly RequestDelegate _next;

        public ApiExceptionHandlingMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            }
            catch (Exception ex) {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception) {
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = exception switch {
                INotFoundException => StatusCodes.Status404NotFound,
                IAlreadyExistsException => StatusCodes.Status409Conflict,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var result = new {
                error = exception.GetType().Name,
                message = exception.Message
            };

            return context.Response.WriteAsJsonAsync(result);
        }
    }
}
