using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Uploads a single file to a destination (file!) on the server
// `sendFile` (string), `destination` (string)
#endregion

public class SendFilePenguin(WaddleContext context, WaddleServerContext serverContext) : PenguinBase
{
    public required string Source;
    public required string Destination;

    public override async Task Execute(CancellationToken cancellationToken)
    {
        await using FileStream fs = File.OpenRead(Source);
        await SftpUtils.CreateDirectoryRecursive(
            context,
            serverContext,
            Path.GetDirectoryName(Destination)
                ?? throw new InvalidOperationException("Invalid destination path"),
            cancellationToken
        );
        await serverContext.SftpClient.UploadFileAsync(fs, Destination, cancellationToken);
    }
}
