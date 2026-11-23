using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace BattleTanks_Backend.Services;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken token)
    {
        logger.LogError(error, "Request failed: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 500, Title = "The request could not be completed", Extensions = { { "traceId", context.TraceIdentifier } } }, token);
        return true;
    }
}
