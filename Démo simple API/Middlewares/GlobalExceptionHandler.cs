using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Démo_simple_API.MiddleWares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync( HttpContext httpContext,Exception exception,CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails();

            switch (exception)
            {
                case ArgumentException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = exception.Message;
                    break;

                case KeyNotFoundException:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = exception.Message;
                    break;

                default:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Une erreur interne est survenue";
                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}