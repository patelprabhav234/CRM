using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRM.Api.Filters;

/// <summary>Returns 503 when PostgreSQL (or the configured host) is not accepting connections.</summary>
public sealed class DbUnavailableExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (!DatabaseExceptionHelper.IsTransientConnectionFailure(context.Exception))
            return;

        context.Result = new ObjectResult(new
        {
            detail =
                "Database server is not running or not reachable. Start PostgreSQL on the port in ConnectionStrings:DefaultConnection " +
                "(see appsettings.Development.json). Example: docker run --name fireops-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=fireops_crm_dev -p 5432:5432 -d postgres:16 — then restart the API.",
        })
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
        };
        context.ExceptionHandled = true;
    }
}
