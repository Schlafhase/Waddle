using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins.ServerPenguins
{
    public class ReceiveFilePenguin(WaddleContext context, WaddleServerContext serverContext) : PenguinBase
    {
        public required string Source;
        public required string Destination;

        public override async Task Execute(CancellationToken cancellationToken)
        {
            context.Logger?.LogTrace("Downloading file {file} to {dest}", Source, Destination);
            Directory.CreateDirectory(
                Path.GetDirectoryName(Destination)
                    ?? throw new InvalidOperationException("Invalid Destination path")
            );
            File.Delete(Destination);
            await using FileStream fs = File.OpenWrite(Destination);
            await serverContext.SftpClient.DownloadFileAsync(Source, fs, cancellationToken);
        }
    }
}