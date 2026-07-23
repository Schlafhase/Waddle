using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins;

public static class SftpUtils
{
    public static async Task CreateDirectoryRecursive(
        WaddleContext context,
        WaddleServerContext serverContext,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (await serverContext.SftpClient.ExistsAsync(path, cancellationToken))
        {
            return;
        }
        DirectoryInfo? parent = Directory.GetParent(path);
        if (
            parent is not null
            && !await serverContext.SftpClient.ExistsAsync(parent.FullName, cancellationToken)
        )
        {
            context.Logger?.LogTrace("Creating remote directory {dir}", parent.FullName);
            await CreateDirectoryRecursive(
                context,
                serverContext,
                parent.FullName,
                cancellationToken
            );
        }
        await serverContext.SftpClient.CreateDirectoryAsync(path, cancellationToken);
    }

    public static string CombinePath(string a, string b, char separator)
    {
        return a.TrimEnd(separator) + separator + b;
    }
}
