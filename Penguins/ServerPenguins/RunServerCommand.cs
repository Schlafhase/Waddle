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

public class RunServerCommand(WaddleContext context) : AsyncPenguin(context)
{
    public required string Command { get; init; }
    public string? Output { get; private set; }
    public int? ExitStatus { get; private set; }
    public bool Ran { get; private set; }

    public override async Task Execute()
    {
        using SshCommand cmd = Context.SshClient.CreateCommand(Command);
        // cmd.CommandTimeout = TimeSpan.FromMinutes(5);

        await cmd.ExecuteAsync(Context.CancellationToken);

        await Context.ServerOutputWriter.WriteAsync(cmd.Result);
        Output = cmd.Result;
        ExitStatus = cmd.ExitStatus;
        Ran = true;

        if (ExitStatus is not null && ExitStatus != 0)
        {
            throw new ServerCommandException(cmd.Error);
        }
    }
}
