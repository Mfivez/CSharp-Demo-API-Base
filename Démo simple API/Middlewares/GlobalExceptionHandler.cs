using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Démo_simple_API.MiddleWares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
    CancellationToken cancellationToken)
        {
            var statusCode = StatusCodes.Status500InternalServerError;
            var title = "Une erreur interne est survenue";

            if (exception is ArgumentException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                title = exception.Message;
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title
            };

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
