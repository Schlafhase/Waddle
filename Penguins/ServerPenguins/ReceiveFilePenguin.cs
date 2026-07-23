using Microsoft.Extensions.Logging;
using Penguins.ServerPenguins;
using Waddle.Config;
using Waddle.Config.Exceptions;

#region ReadmeInfo
// Downloads a single file from the server to a destination (file!) on the client
// `receiveFile` (string), `destination` (string)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["ReceiveFile", "Destination"], "ReceiveFilePenguin")]
        public IPenguin MatchReceiveFilePenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { ReceiveFile: string receiveFile, Destination: string destination } =>
                    new ReceiveFilePenguin(context, context.ServerOrThrow)
                    {
                        Name = yp.Name,
                        Source = receiveFile,
                        Destination = destination,
                    },
                _ => throw new NoMatchException(),
            };
        }
    }
}

namespace Penguins.ServerPenguins
{
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
}
