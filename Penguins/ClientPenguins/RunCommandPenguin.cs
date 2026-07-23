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

#region ReadmeInfo
// Runs a command on the client using the value of `shell` as shell or `sh` (Linux) or `cmd.exe` (Windows). `shell` must be something like `["sh", "-c"]` (in yaml syntax of course). Specify `variable` to store the output in a variable.
// `cmd` (string), `shell` (List\<string\>), `variable` (string)
#endregion

public class RunCommandPenguin(WaddleContext context) : PenguinBase(context)
{
    [Interpolated]
    public required string Command { get; init; }

    [Interpolated]
    public List<string>? Shell { get; init; }

    public string? Output { get; private set; }
    public int? ExitStatus { get; private set; }

    public override async Task Execute(CancellationToken cancellationToken)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        var shell = Shell ?? _context.Config.DefaultShell;

        ProcessStartInfo psi = new()
        {
            FileName = shell?[0] ?? (isWindows ? "cmd.exe" : "/bin/sh"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
        };
        if (shell is not null)
        {
            foreach (string arg in shell.Skip(1))
            {
                psi.ArgumentList.Add(arg);
                psi.ArgumentList.Add(Command);
            }
        }
        else if (isWindows)
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

        await _context.ClientOutputWriter.WriteAsync(output);
        _context.Logger?.LogTrace("Local command result: {output}", output);

        if (ExitStatus is not null && ExitStatus != 0)
        {
            throw new CommandException(error);
        }
    }
}
