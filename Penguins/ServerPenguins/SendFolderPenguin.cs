using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins.ServerPenguins;

public class SendFolderPenguin(WaddleContext context, WaddleServerContext serverContext)
    : PenguinBase
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
        await SftpUtils.CreateDirectoryRecursive(context, serverContext, destination, cancellationToken);
        // Upload files
        foreach (string f in Directory.EnumerateFiles(path))
        {
            context.Logger.LogTrace("Uploading {file} to {destination}", f, destination);
            context.Status = $"Sending {Path.GetRelativePath(Directory.GetCurrentDirectory(), f)}";
            try
            {
                await using FileStream fs = File.OpenRead(f);
                await serverContext.SftpClient.UploadFileAsync(
                    fs,
                    Path.Combine(destination, Path.GetFileName(f)),
                    cancellationToken
                );
            }
            catch (FileNotFoundException)
            {
                context.Logger.LogWarning(
                    "Skipping file {file} because the file was not found.",
                    f
                );
            }
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
