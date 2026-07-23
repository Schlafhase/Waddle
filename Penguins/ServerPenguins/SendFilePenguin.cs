using Penguins.ServerPenguins;
using Waddle.Config;
using Waddle.Config.Exceptions;

#region ReadmeInfo
// Uploads a single file to a destination (file!) on the server
// `sendFile` (string), `destination` (string)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["SendFile", "Destination"], "SendFilePenguin")]
        public IPenguin MatchSendFilePenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { SendFile: string sendFile, Destination: string destination } =>
                    new SendFilePenguin(context, context.ServerOrThrow)
                    {
                        Name = yp.Name,
                        Source = sendFile,
                        Destination = destination,
                    },
                _ => throw new NoMatchException(),
            };
        }
    }
}

namespace Penguins.ServerPenguins
{
    public class SendFilePenguin(WaddleContext context, WaddleServerContext serverContext)
        : ServerPenguinBase(context, serverContext)
    {
        public required string Source;
        public required string Destination;

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await using FileStream fs = File.OpenRead(Source);
            await SftpUtils.CreateDirectoryRecursive(
                _context,
                _serverContext,
                Path.GetDirectoryName(Destination)
                    ?? throw new InvalidOperationException("Invalid destination path"),
                cancellationToken
            );
            await _serverContext.SftpClient.UploadFileAsync(fs, Destination, cancellationToken);
        }
    }
}
