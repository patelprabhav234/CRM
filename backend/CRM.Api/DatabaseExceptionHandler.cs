using Microsoft.AspNetCore.Diagnostics;
using CRM.Infrastructure.Persistence;

namespace CRM.Api;

/// <summary>
/// Maps DB connection failures to HTTP 503 with a clear JSON body (works with EF/Npgsql wrappers).
/// </summary>
public sealed class DatabaseExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!DatabaseExceptionHelper.IsTransientConnectionFailure(exception))
            return false;

        if (http.Response.HasStarted)
            return false;

        http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        http.Response.ContentType = "application/json; charset=utf-8";
        await http.Response.WriteAsJsonAsync(
            new
            {
                detail =
                    "Database server is not running or not reachable. " +
                    "From the repo root run: docker compose up -d — then restart the API. " +
                    "Connection is configured in appsettings.Development.json (ConnectionStrings:DefaultConnection).",
            },
            cancellationToken);
        return true;
    }
}
