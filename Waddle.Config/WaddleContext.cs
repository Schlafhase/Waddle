using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using Renci.SshNet;
using SshNet.Agent;

namespace Waddle.Config;

public sealed class WaddleContext : IAsyncDisposable, IDisposable
{
    public const string Version = "0.3.1";

    public required WaddleConfig Config;

    public required SshClient SshClient;
    public required SftpClient SftpClient;

    public required Stream ServerOutput;
    public readonly StreamWriter ServerOutputWriter;
    public required Stream ClientOutput;
    public readonly StreamWriter ClientOutputWriter;

    public required ILogger Logger;

    public Action? OnStatusChange;

    public string? Status
    {
        get => field;
        set
        {
            field = value;
            OnStatusChange?.Invoke();
        }
    }

    private readonly ILoggerFactory _loggerFactory;

    [SetsRequiredMembers]
    public WaddleContext(WaddleConfig cfg, Func<string> getPassword)
    {
        AuthenticationMethod method = cfg switch
        {
            { UsePassword: true } => new PasswordAuthenticationMethod(cfg.Username, getPassword()),
            { KeyfileFullPath: not null } => new PrivateKeyAuthenticationMethod(
                cfg.Username,
                new PrivateKeyFile(cfg.KeyfileFullPath)
            ),
            { UseSshAgent: true } => new PrivateKeyAuthenticationMethod(
                cfg.Username,
                new SshAgent(
                    Environment.GetEnvironmentVariable("SSH_AUTH_SOCK")
                        ?? throw new InvalidOperationException(
                            "Environment variable SSH_AUTH_SOCK must be set when using SSH Agent"
                        )
                ).RequestIdentities()
            ),
            _ => throw new NotImplementedException(),
        };

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            if (cfg.LogFileName is not null)
            {
                builder.AddFile(cfg.LogFileName);
            }
            builder.AddFilter("Waddle", cfg.LogLevel);
        });
        ConnectionInfo info = new(cfg.Host, cfg.Port, cfg.Username, method)
        {
            LoggerFactory = _loggerFactory,
        };

        Config = cfg;

        SshClient = new(info);
        SftpClient = new(info);

        ServerOutput = cfg.ServerOutputFileName is not null
            ? new FileStream(cfg.ServerOutputFileName, FileMode.Create)
            : new MemoryStream();
        ServerOutputWriter = new(ServerOutput);

        ClientOutput = cfg.ClientOutputFileName is not null
            ? new FileStream(cfg.ClientOutputFileName, FileMode.Create)
            : new MemoryStream();
        ClientOutputWriter = new(ClientOutput);

        Logger = _loggerFactory.CreateLogger("Waddle");
    }

    public async Task Initialise()
    {
        await SshClient.ConnectAsync(CancellationToken.None);
        await SftpClient.ConnectAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        SshClient.Disconnect();
        SshClient.Dispose();
        SftpClient.Disconnect();
        SftpClient.Dispose();

        ServerOutputWriter.Flush();
        ServerOutput.Dispose();

        ClientOutputWriter.Flush();
        ClientOutput.Dispose();

        _loggerFactory.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        SshClient.Disconnect();
        SftpClient.Disconnect();
        SshClient.Dispose();
        SftpClient.Dispose();

        await ServerOutputWriter.FlushAsync();
        await ServerOutput.DisposeAsync();

        await ClientOutputWriter.FlushAsync();
        await ClientOutput.DisposeAsync();

        _loggerFactory.Dispose();
    }
}
