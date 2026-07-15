using System.Diagnostics.CodeAnalysis;
using Renci.SshNet;
using SshNet.Agent;

namespace Waddle.Config;

public struct WaddleContext : IDisposable
{
    public required WaddleConfig Config;
    public required SshClient SshClient;
    public required SftpClient SftpClient;
    public CancellationToken CancellationToken = CancellationToken.None;
    public required Stream ServerOutput;
    public readonly StreamWriter ServerOutputWriter;

    [SetsRequiredMembers]
    public WaddleContext(WaddleConfig cfg, Func<string> getPassword)
    {
        AuthenticationMethod method = cfg switch
        {
            { UsePassword: true } => new PasswordAuthenticationMethod(cfg.Username, getPassword()),
            { Keyfile: not null } => new PrivateKeyAuthenticationMethod(
                cfg.Username,
                new PrivateKeyFile(cfg.Keyfile)
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

        ConnectionInfo info = new(cfg.Host, cfg.Port, cfg.Username, method);

        Config = cfg;
        SshClient = new(info);
        SftpClient = new(info);
        ServerOutput = cfg.ServerOutputFileName is not null
            ? new FileStream(cfg.ServerOutputFileName, FileMode.Create)
            : new MemoryStream();
        ServerOutputWriter = new(ServerOutput);
    }

    public readonly async Task Initialise()
    {
        await SshClient.ConnectAsync(CancellationToken);
        await SftpClient.ConnectAsync(CancellationToken);
    }

    public readonly void Dispose()
    {
        SshClient.Disconnect();
        SshClient.Dispose();
        SftpClient.Disconnect();
        SftpClient.Dispose();

        ServerOutputWriter.Flush();
        ServerOutput.Dispose();
    }
}