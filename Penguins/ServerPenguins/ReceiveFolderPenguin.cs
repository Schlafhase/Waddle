using Microsoft.Extensions.Logging;
using Penguins.ServerPenguins;
using Renci.SshNet.Sftp;
using Waddle.Config;
using Waddle.Config.Exceptions;

#region ReadmeInfo
// Downloads a folder from the server to a destination (directory!) on the client
// `receiveFolder` (string), `destination` (string)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["ReceiveFolder", "Destination"], "ReceiveFolderPenguin")]
        public IPenguin MatchReceiveFolderPenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { ReceiveFolder: string receiveFolder, Destination: string destination } =>
                    new ReceiveFolderPenguin(context, context.ServerOrThrow)
                    {
                        Name = yp.Name,
                        Source = receiveFolder,
                        Destination = destination,
                    },
                _ => throw new NoMatchException(),
            };
        }
    }
}

namespace Penguins.ServerPenguins
{
    public class ReceiveFolderPenguin(WaddleContext context, WaddleServerContext serverContext)
        : ServerPenguinBase(context, serverContext)
    {
        public required string Source;
        public required string Destination;

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await downloadDirectory(Source, Destination, cancellationToken);
        }

        private async Task downloadDirectory(
            string source,
            string destination,
            CancellationToken cancellationToken
        )
        {
            await foreach (
                ISftpFile file in _serverContext.SftpClient.ListDirectoryAsync(
                    source,
                    cancellationToken
                )
            )
            {
                if (file.Name is "." or "..")
                {
                    continue;
                }
                if (file.IsDirectory)
                {
                    await downloadDirectory(
                        file.FullName,
                        SftpUtils.CombinePath(
                            destination,
                            Path.GetRelativePath(source, file.FullName),
                            _serverContext.Config.DirectorySeparator
                        ),
                        cancellationToken
                    );
                    continue;
                }

                string destinationPath = Path.GetFullPath(Path.Combine(destination, file.Name));
                _context.Logger?.LogTrace(
                    "Downloading file {file} to {dest}",
                    file.FullName,
                    destinationPath
                );

                Status = $"Downloading {file.FullName}";
                Directory.CreateDirectory(destination);
                File.Delete(destinationPath);
                await using FileStream fs = File.OpenWrite(destinationPath);
                await _serverContext.SftpClient.DownloadFileAsync(
                    file.FullName,
                    fs,
                    cancellationToken
                );
            }
        }
    }
}
