using System.Net.Sockets;

namespace CRM.Api;

/// <summary>Detects PostgreSQL / network failures when the server is down or unreachable.</summary>
public static class DatabaseExceptionHelper
{
    public static bool IsTransientConnectionFailure(Exception? ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SocketException se)
            {
                if (se.SocketErrorCode is SocketError.ConnectionRefused or SocketError.HostUnreachable)
                    return true;
                if (se.ErrorCode == 10061) // WSAECONNREFUSED (Windows)
                    return true;
            }
        }

        if (ex?.Message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }
}
