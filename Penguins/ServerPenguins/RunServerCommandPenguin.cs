using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Waddle.Config;

namespace Penguins.ServerPenguins;

#region ReadmeInfo
// Runs a command on the on the server via SSH
// `serverCmd` (string)
#endregion

public class ServerCommandException : Exception
{
    public ServerCommandException() { }

    public ServerCommandException(string? message)
        : base(message) { }

    public ServerCommandException(string? message, Exception? innerException)
        : base(message, innerException) { }
}

public class RunServerCommandPenguin(WaddleContext context, WaddleServerContext serverContext)
    : ServerPenguinBase(context, serverContext)
{
    public required string Command { get; init; }
    public int? ExitStatus { get; private set; }

    public override async Task Execute(CancellationToken cancellationToken)
    {
        using SshCommand cmd = _serverContext.SshClient.CreateCommand(Command);
        await cmd.ExecuteAsync(cancellationToken);

        string output = cmd.Result + cmd.Error;
        _context.Logger?.LogTrace("Remote command output: {output}", output);
        await _serverContext.ServerOutputWriter.WriteAsync(output);
        ExitStatus = cmd.ExitStatus;

        if (ExitStatus is not null && ExitStatus != 0)
        {
            throw new ServerCommandException(cmd.Error);
        }
    }
}