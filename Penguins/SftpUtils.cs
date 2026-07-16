using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins;

public static class SftpUtils
{
    public static async Task CreateDirectoryRecursive(
        WaddleContext context,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (await context.SftpClient.ExistsAsync(path, cancellationToken))
        {
            return;
        }
        DirectoryInfo? parent = Directory.GetParent(path);
        if (
            parent is not null
            && !await context.SftpClient.ExistsAsync(parent.FullName, cancellationToken)
        )
        {
            context.Logger.LogTrace("Creating remote directory {dir}", parent.FullName);
            await CreateDirectoryRecursive(context, parent.FullName, cancellationToken);
        }
        await context.SftpClient.CreateDirectoryAsync(path, cancellationToken);
    }
}
