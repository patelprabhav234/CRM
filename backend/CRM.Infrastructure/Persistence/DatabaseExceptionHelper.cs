using System.Net.Sockets;
using Npgsql;

namespace CRM.Infrastructure.Persistence;

/// <summary>
/// Detects PostgreSQL / network failures when the server is down or unreachable.
/// EF Core often wraps these in <see cref="InvalidOperationException"/> ("transient failure");
/// Npgsql wraps <see cref="SocketException"/>.
/// </summary>
public static class DatabaseExceptionHelper
{
    public static bool IsTransientConnectionFailure(Exception? ex) => IsTransientConnectionFailure(ex, 0);

    private static bool IsTransientConnectionFailure(Exception? ex, int depth)
    {
        if (ex is null || depth > 24)
            return false;

        if (Matches(ex))
            return true;

        if (ex is AggregateException agg)
        {
            foreach (var inner in agg.Flatten().InnerExceptions)
            {
                if (IsTransientConnectionFailure(inner, depth + 1))
                    return true;
            }
        }

        return IsTransientConnectionFailure(ex.InnerException, depth + 1);
    }

    private static bool Matches(Exception e)
    {
        if (e is SocketException se && IsConnectionRefused(se))
            return true;

        if (e is NpgsqlException npg)
        {
            if (npg.InnerException is SocketException sex && IsConnectionRefused(sex))
                return true;
            if (npg.Message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase))
                return true;
            if (npg.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // EF Core: "An exception has been raised that is likely due to a transient failure."
        if (e is InvalidOperationException ioe &&
            ioe.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase))
            return true;

        if (e.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase))
            return true;

        if (e.Message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsConnectionRefused(SocketException se) =>
        se.SocketErrorCode is SocketError.ConnectionRefused or SocketError.HostUnreachable
        || se.ErrorCode == 10061; // WSAECONNREFUSED (Windows)
}
