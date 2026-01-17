using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IntegratedAPI.Exceptions
{
    public class ProductExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {

            var problemDetails = new ProblemDetails();

            if (exception is NoProductsException ex)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = ex.message;
                problemDetails.Status = ex.statusCode;
                problemDetails.Detail = exception.Message;
            }
            else
            {
                problemDetails.Title = exception.Message;
                problemDetails.Status = httpContext.Response.StatusCode;
            }

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
            return true;
        }
    }
}
