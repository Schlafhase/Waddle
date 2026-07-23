using Penguins.ClientPenguins;
using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Sends a folder as a tar archive and extracts it on the server. Requires `tar` to be installed on both the client **and** the server
// `sendCompressed` (string), `destination` (string)
#endregion

// Implemented as predefined RunWorkflowPenguin
public static class SendCompressedFolderPenguin
{
    public static RunWorkflowPenguin New(
        WaddleContext context,
        WaddleServerContext serverContext,
        string name,
        string source,
        string destination
    )
    {
        string archiveName = Guid.NewGuid().ToString() + ".tar.gz";
        List<YamlPenguin> workflow =
        [
            new YamlPenguin
            {
                Name = "Compress files",
                Cmd = $"tar -czvf {archiveName} -C {source} .",
            },
            new YamlPenguin
            {
                Name = "Send archive",
                SendFile = archiveName,
                Destination = SftpUtils.CombinePath(
                    destination,
                    archiveName,
                    serverContext.Config.DirectorySeparator
                ),
            },
            new YamlPenguin
            {
                Name = "Decompress files",
                ServerCmd =
                    $"tar -xzvf {SftpUtils.CombinePath(destination, archiveName, serverContext.Config.DirectorySeparator)} -C {destination}",
                IgnoreError = true,
            },
            new YamlPenguin
            {
                Name = "Clean up client",
                Cmd = $"rm {archiveName}",
                IgnoreError = true,
            },
            new YamlPenguin
            {
                Name = "Clean up server",
                ServerCmd =
                    $"rm {SftpUtils.CombinePath(destination, archiveName, serverContext.Config.DirectorySeparator)}",
            },
        ];
        return new(context, workflow) { Name = name };
    }
}