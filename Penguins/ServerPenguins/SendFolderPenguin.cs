using Microsoft.Extensions.Logging;

using Penguins.ServerPenguins;

using Waddle.Config;
using Waddle.Config.Exceptions;


#region ReadmeInfo
// Uploads a folder to a destination (directory!) on the server
// `sendFolder` (string), `destination` (string)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["SendFolder", "Destination"], "SendFolderPenguin")]
        public IPenguin MatchSendFolderPenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { SendFolder: string sendFolder, Destination: string destination } =>
                    new SendFolderPenguin(context, context.ServerOrThrow)
                    {
                        Name = yp.Name,
                        Source = sendFolder,
                        Destination = destination,
                    },
                _ => throw new NoMatchException(),
            };
        }
    }
}

namespace Penguins.ServerPenguins
{
    public class SendFolderPenguin(WaddleContext context, WaddleServerContext serverContext)
        : ServerPenguinBase(context, serverContext)
    {
        public required string Source;
        public required string Destination;

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await sendFolder(Source, Destination, cancellationToken);
        }

        private async Task sendFolder(
            string path,
            string destination,
            CancellationToken cancellationToken
        )
        {
            await SftpUtils.CreateDirectoryRecursive(
                _context,
                _serverContext,
                destination,
                cancellationToken
            );
            // Upload files
            foreach (string f in Directory.EnumerateFiles(path))
            {
                _context.Logger?.LogTrace("Uploading {file} to {destination}", f, destination);
                Status = $"Sending {Path.GetRelativePath(Directory.GetCurrentDirectory(), f)}";
                try
                {
                    await using FileStream fs = File.OpenRead(f);
                    await _serverContext.SftpClient.UploadFileAsync(
                        fs,
                        SftpUtils.CombinePath(
                            destination,
                            Path.GetFileName(f),
                            _serverContext.Config.DirectorySeparator
                        ),
                        cancellationToken
                    );
                }
                catch (FileNotFoundException)
                {
                    _context.Logger?.LogWarning(
                        "Skipping file {file} because the file was not found.",
                        f
                    );
                }
            }
            // Upload subdirectories
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                await sendFolder(
                    dir,
                    SftpUtils.CombinePath(
                        destination,
                        Path.GetRelativePath(path, dir),
                        _serverContext.Config.DirectorySeparator
                    ),
                    cancellationToken
                );
            }
        }
    }
}