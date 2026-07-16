using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins.ServerPenguins
{
    public class ReceiveFilePenguin(WaddleContext context) : PenguinBase(context)
    {
        public required string Source;
        public required string Destination;

        public override async Task Execute(CancellationToken cancellationToken)
        {
            Context.Logger.LogTrace("Downloading file {file} to {dest}", Source, Destination);
            Directory.CreateDirectory(
                Path.GetDirectoryName(Destination)
                    ?? throw new InvalidOperationException("Invalid Destination path")
            );
            File.Delete(Destination);
            await using FileStream fs = File.OpenWrite(Destination);
            await Context.SftpClient.DownloadFileAsync(Source, fs, cancellationToken);
        }
    }
}
