using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Uploads a folder to a destination (directory!) on the server
// `sendFolder` (string), `destination` (string)
#endregion

public class SendFolderPenguin(WaddleContext context, WaddleServerContext serverContext)
    : ServerPenguinBase(context, serverContext)
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
        await SftpUtils.CreateDirectoryRecursive(
            _context,
            _serverContext,
            destination,
            cancellationToken
        );
        // Upload files
        foreach (string f in Directory.EnumerateFiles(path))
        {
            _context.Logger?.LogTrace("Uploading {file} to {destination}", f, destination);
            Status = $"Sending {Path.GetRelativePath(Directory.GetCurrentDirectory(), f)}";
            try
            {
                await using FileStream fs = File.OpenRead(f);
                await _serverContext.SftpClient.UploadFileAsync(
                    fs,
                    SftpUtils.CombinePath(
                        destination,
                        Path.GetFileName(f),
                        _serverContext.Config.DirectorySeparator
                    ),
                    cancellationToken
                );
            }
            catch (FileNotFoundException)
            {
                _context.Logger?.LogWarning(
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
                SftpUtils.CombinePath(
                    destination,
                    Path.GetRelativePath(path, dir),
                    _serverContext.Config.DirectorySeparator
                ),
                cancellationToken
            );
        }
    }
}