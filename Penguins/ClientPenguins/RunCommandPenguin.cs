using System.Diagnostics;
using Waddle.Config;

namespace Penguins.ClientPenguins;

public class CommandException : Exception
{
    public CommandException() { }

    public CommandException(string? message)
        : base(message) { }

    public CommandException(string? message, Exception? innerException)
        : base(message, innerException) { }
}

public class RunCommandPenguin(WaddleContext context) : PenguinBase(context)
{
    public required string Command { get; init; }
    public string? Output { get; private set; }
    public int? ExitStatus { get; private set; }

    public override async Task Execute(CancellationToken cancellationToken)
    {
        string commandString = Command.Trim();
        string command = "";
        string arguments = "";
        int firstSpace = commandString.IndexOf(' ');

        if (firstSpace != -1)
        {
            command = commandString[..firstSpace];
            arguments = commandString[(firstSpace + 1)..];
        }

        ProcessStartInfo psi = new()
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };

        using Process? p =
            Process.Start(psi) ?? throw new CommandException("The command could not be started");

        string output = await p.StandardOutput.ReadToEndAsync(cancellationToken);
        await p.WaitForExitAsync(cancellationToken);

        Output = output;
        ExitStatus = p.ExitCode;

       await Context.ClientOutputWriter.WriteAsync(output);

        if (ExitStatus is not null && ExitStatus != 0)
        {
            throw new CommandException(output);
        }
    }
}
