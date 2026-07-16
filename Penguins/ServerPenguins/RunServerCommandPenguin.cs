using Microsoft.Extensions.Logging;

using Renci.SshNet;
using Waddle.Config;

namespace Penguins.ServerPenguins;

public class ServerCommandException : Exception
{
    public ServerCommandException() { }

    public ServerCommandException(string? message)
        : base(message) { }

    public ServerCommandException(string? message, Exception? innerException)
        : base(message, innerException) { }
}

public class RunServerCommandPenguin(WaddleContext context) : PenguinBase(context)
{
    public required string Command { get; init; }
    public string? Output { get; private set; }
    public int? ExitStatus { get; private set; }

    public override async Task Execute(CancellationToken cancellationToken)
    {
        using SshCommand cmd = Context.SshClient.CreateCommand(Command);
        await cmd.ExecuteAsync(cancellationToken);

        await Context.ServerOutputWriter.WriteAsync(cmd.Result);
        Context.Logger.LogTrace("Remote command output: {output}", cmd.Result);
        Output = cmd.Result;
        ExitStatus = cmd.ExitStatus;

        if (ExitStatus is not null && ExitStatus != 0)
        {
            throw new ServerCommandException(cmd.Error);
        }
    }
}