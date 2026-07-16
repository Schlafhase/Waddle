using Waddle.Config;

namespace Penguins.ServerPenguins;

public class SendFilePenguin(WaddleContext context) : PenguinBase(context)
{
    public required string Source;
    public required string Destination;

    public override async Task Execute(CancellationToken cancellationToken)
    {
        await using FileStream fs = File.OpenRead(Source);
        await SftpUtils.CreateDirectoryRecursive(
            Context,
            Path.GetDirectoryName(Destination)
                ?? throw new InvalidOperationException("Invalid destination path"),
            cancellationToken
        );
        await Context.SftpClient.UploadFileAsync(fs, Destination, cancellationToken);
    }
}
