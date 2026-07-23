using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Downloads a single file from the server to a destination (file!) on the client
// `receiveFile` (string), `destination` (string)
#endregion

public class ReceiveFilePenguin(WaddleContext context, WaddleServerContext serverContext)
    : ServerPenguinBase(context, serverContext)
{
    public required string Source;
    public required string Destination;

    public override async Task Execute(CancellationToken cancellationToken)
    {
        _context.Logger?.LogTrace("Downloading file {file} to {dest}", Source, Destination);
        Directory.CreateDirectory(
            Path.GetDirectoryName(Destination)
                ?? throw new InvalidOperationException("Invalid Destination path")
        );
        File.Delete(Destination);
        await using FileStream fs = File.OpenWrite(Destination);
        await _serverContext.SftpClient.DownloadFileAsync(Source, fs, cancellationToken);
    }
}