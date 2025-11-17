using System.Net;
using System.Text.Json;
using FluentValidation;
using Backend.Exceptions;

namespace Backend.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); 
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            object errorResponse;

            switch (exception)
            {
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        message = "Validation failed",
                        errors = validationException.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            )
                    };
                    break;

                case DuplicateEmailException:
                    statusCode = HttpStatusCode.Conflict; // 409
                    errorResponse = new { message = exception.Message };
                    break;

                case UserNotFoundException:
                    statusCode = HttpStatusCode.NotFound; // 404
                    errorResponse = new { message = exception.Message };
                    break;

                case InvalidCredentialsException:
                    statusCode = HttpStatusCode.Unauthorized; // 401
                    errorResponse = new { message = exception.Message };
                    break;

                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound; // generic 404
                    errorResponse = new { message = exception.Message };
                    break;

                default:
                    errorResponse = new { message = exception.Message };
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
