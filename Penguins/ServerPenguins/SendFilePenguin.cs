using Waddle.Config;

namespace Penguins.ServerPenguins;

public class SendFilePenguin(WaddleContext context) : PenguinBase
{
    public required string Source;
    public required string Destination;

    public override async Task Execute(CancellationToken cancellationToken)
    {
        await using FileStream fs = File.OpenRead(Source);
        await SftpUtils.CreateDirectoryRecursive(
            context,
            Path.GetDirectoryName(Destination)
                ?? throw new InvalidOperationException("Invalid destination path"),
            cancellationToken
        );
        await context.SftpClient.UploadFileAsync(fs, Destination, cancellationToken);
    }
}