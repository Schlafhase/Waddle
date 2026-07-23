using Penguins.ClientPenguins;
using Penguins.ServerPenguins;
using Waddle.Config;
using Waddle.Config.Exceptions;

#region ReadmeInfo
// Sends a folder as a tar archive and extracts it on the server. Requires `tar` to be installed on both the client **and** the server
// `sendCompressed` (string), `destination` (string)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["SendCompressed", "Destination"], "SendCompressedFolderPenguin")]
        public IPenguin MatchSendCompressedFolderPenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { SendCompressed: string sendCompressed, Destination: string destination } =>
                    SendCompressedFolderPenguin.New(
                        context,
                        context.ServerOrThrow,
                        yp.Name,
                        sendCompressed,
                        destination
                    ),
                _ => throw new NoMatchException(),
            };
        }
    }
}

namespace Penguins.ServerPenguins
{
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
}
