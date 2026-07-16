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
        await SftpUtils.CreateDirectoryRecursive(Context, destination, cancellationToken);
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

}
