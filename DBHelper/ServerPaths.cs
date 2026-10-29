using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DBHelper;

/// <summary>Paths on the server's file system, which is Linux's or Windows'.</summary>
public static class ServerPaths
{
    /// <summary>Does the server run on Linux?</summary>
    public static bool IsLinux(Server server) =>
        string.Equals(server.HostPlatform, HostPlatformNames.Linux, StringComparison.OrdinalIgnoreCase);

    /// <summary>A file in a directory on the server, joined with the server's separator.</summary>
    public static string Combine(Server server, string directory, string fileName)
    {
        var separator = IsLinux(server) ? "/" : @"\";
        return directory.EndsWith(separator, StringComparison.Ordinal) ? directory + fileName : directory + separator + fileName;
    }
}
