using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Uploads a single file to a destination (file!) on the server
// `sendFile` (string), `destination` (string)
#endregion

public class SendFilePenguin(WaddleContext context, WaddleServerContext serverContext) : ServerPenguinBase(context, serverContext)
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