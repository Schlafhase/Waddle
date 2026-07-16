using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
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
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        ProcessStartInfo psi = new()
        {
            FileName = isWindows ? "cmd.exe" : "/bin/sh",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
        };
        if (isWindows)
        {
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(Command);
        }
        else
        {
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(Command);
        }

        using Process? p =
            Process.Start(psi) ?? throw new CommandException("The command could not be started");

        string output = await p.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await p.StandardError.ReadToEndAsync(cancellationToken);
        await p.WaitForExitAsync(cancellationToken);

        output += error;

        Output = output;
        ExitStatus = p.ExitCode;

        await Context.ClientOutputWriter.WriteAsync(output);
        Context.Logger.LogTrace("Local command result: {output}", output);

        if (ExitStatus is not null && ExitStatus != 0)
        {
            throw new CommandException(output);
        }
    }
}
