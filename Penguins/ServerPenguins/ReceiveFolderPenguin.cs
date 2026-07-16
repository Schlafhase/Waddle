using Microsoft.Extensions.Logging;

using Renci.SshNet.Sftp;
using Waddle.Config;

namespace Penguins.ServerPenguins
{
    public class ReceiveFolderPenguin(WaddleContext context) : PenguinBase(context)
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
                ISftpFile file in Context.SftpClient.ListDirectoryAsync(source, cancellationToken)
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
                        Path.Combine(destination, Path.GetRelativePath(source, file.FullName)),
                        cancellationToken
                    );
                    continue;
                }

                Context.Logger.LogTrace("Downloading file {file} to {dest}", file.FullName, destination);
                Directory.CreateDirectory(destination);
                await Context.SftpClient.DownloadFileAsync(
                    file.FullName,
                    File.OpenWrite(Path.Combine(destination, file.Name)),
                    cancellationToken
                );
            }
        }
    }
}
