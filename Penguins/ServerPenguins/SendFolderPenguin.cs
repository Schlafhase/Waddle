using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins.ServerPenguins;

public class SendFolderPenguin(WaddleContext context) : PenguinBase(context)
{
    public required string Source;
    public required string Destination;

    public override async Task Execute(CancellationToken cancellationToken)
    {
        await sendFolder(Source, Destination, cancellationToken);
    }

    private async Task sendFolder(
        string path,
        string destination,
        CancellationToken cancellationToken
    )
    {
        await createDirectoryRecursive(destination, cancellationToken);
        // Upload files
        foreach (string f in Directory.EnumerateFiles(path))
        {
            Context.Logger.LogTrace("Uploading {file} to {destination}", f, destination);
            await using FileStream fs = File.OpenRead(f);
            await Context.SftpClient.UploadFileAsync(
                fs,
                Path.Combine(destination, Path.GetFileName(f)),
                cancellationToken
            );
        }
        // Upload subdirectories
        foreach (string dir in Directory.EnumerateDirectories(path))
        {
            await sendFolder(
                dir,
                Path.Combine(destination, Path.GetRelativePath(path, dir)),
                cancellationToken
            );
        }
    }

    private async Task createDirectoryRecursive(string path, CancellationToken cancellationToken)
    {
        if (await Context.SftpClient.ExistsAsync(path, cancellationToken))
        {
            return;
        }
        DirectoryInfo? parent = Directory.GetParent(path);
        if (
            parent is not null
            && !await Context.SftpClient.ExistsAsync(parent.FullName, cancellationToken)
        )
        {
            Context.Logger.LogTrace("Creating remote directory {dir}", parent.FullName);
            await createDirectoryRecursive(parent.FullName, cancellationToken);
        }
        await Context.SftpClient.CreateDirectoryAsync(path, cancellationToken);
    }
}
