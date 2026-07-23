using Microsoft.Extensions.Logging;
using Renci.SshNet.Sftp;
using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Downloads a folder from the server to a destination (directory!) on the client
// `receiveFolder` (string), `destination` (string)
#endregion

public class ReceiveFolderPenguin(WaddleContext context, WaddleServerContext serverContext)
    : PenguinBase(context)
{
    public required string Source;
    public required string Destination;

    public override async Task Execute(CancellationToken cancellationToken)
    {
        await downloadDirectory(Source, Destination, cancellationToken);
    }

    private async Task downloadDirectory(
        string source,
        string destination,
        CancellationToken cancellationToken
    )
    {
        await foreach (
            ISftpFile file in serverContext.SftpClient.ListDirectoryAsync(source, cancellationToken)
        )
        {
            if (file.Name is "." or "..")
            {
                continue;
            }
            if (file.IsDirectory)
            {
                await downloadDirectory(
                    file.FullName,
                    SftpUtils.CombinePath(
                        destination,
                        Path.GetRelativePath(source, file.FullName),
                        serverContext.Config.DirectorySeparator
                    ),
                    cancellationToken
                );
                continue;
            }

            string destinationPath = Path.GetFullPath(Path.Combine(destination, file.Name));
            _context.Logger?.LogTrace(
                "Downloading file {file} to {dest}",
                file.FullName,
                destinationPath
            );

            Status = $"Downloading {file.FullName}";
            Directory.CreateDirectory(destination);
            File.Delete(destinationPath);
            await using FileStream fs = File.OpenWrite(destinationPath);
            await serverContext.SftpClient.DownloadFileAsync(file.FullName, fs, cancellationToken);
        }
    }
}
