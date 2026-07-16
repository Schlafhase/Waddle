using Microsoft.Extensions.Logging;
using Renci.SshNet.Sftp;
using Waddle.Config;

namespace Penguins.ServerPenguins
{
    public class ReceiveFolderPenguin(WaddleContext context) : PenguinBase
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
                ISftpFile file in context.SftpClient.ListDirectoryAsync(source, cancellationToken)
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

                string destinationPath = Path.GetFullPath(Path.Combine(destination, file.Name));
                context.Logger.LogTrace(
                    "Downloading file {file} to {dest}",
                    file.FullName,
                    destinationPath
                );
                context.Status = $"Downloading {file.FullName}";
                Directory.CreateDirectory(destination);
                File.Delete(destinationPath);
                await using FileStream fs = File.OpenWrite(destinationPath);
                await context.SftpClient.DownloadFileAsync(file.FullName, fs, cancellationToken);
            }
        }
    }
}
