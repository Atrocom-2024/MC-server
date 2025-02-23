using System.Net;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace MC_server.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (NotSupportedException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest); // 또는 422 Unprocessable Entity
            }
            catch (KeyNotFoundException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
            }
            catch (InvalidOperationException ex) when (context.Request.Path.StartsWithSegments("/api/users"))
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.Conflict);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[web] Error in GlobalExceptionMiddleware: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"[web] Error in GlobalExceptionMiddleware: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[web] Error in GlobalExceptionMiddleware: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[web] Error in GlobalExceptionMiddleware: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[web] Error in GlobalExceptionMiddleware: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
        {
            var response = new
            {
                error = exception.Message,
                statusCode = (int)statusCode
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}
