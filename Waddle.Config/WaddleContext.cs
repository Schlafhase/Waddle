using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace Waddle.Config;

public sealed class WaddleContext : IAsyncDisposable, IDisposable
{
    public static Version Version =>
        Assembly.GetEntryAssembly()?.GetName()?.Version
        ?? throw new MissingFieldException("Project doesn't specify a version");

    public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";

    public required WaddleConfig Config;

    public WaddleServerContext? Server;

    public required Stream ClientOutput;
    public required StreamWriter ClientOutputWriter;

    public ILogger? Logger;


    private readonly ILoggerFactory? _loggerFactory;

    [SetsRequiredMembers]
    public WaddleContext(WaddleConfig cfg, Func<string> getPassword)
    {
        ClientOutput = cfg.ClientOutputFileName is not null
            ? new FileStream(cfg.ClientOutputFileName, FileMode.Create)
            : new MemoryStream();
        ClientOutputWriter = new(ClientOutput);

        _loggerFactory = !string.IsNullOrWhiteSpace(cfg.LogFileName)
            ? LoggerFactory.Create(builder =>
            {
                builder.AddFile(cfg.LogFileName);
                builder.AddFilter("Waddle", cfg.LogLevel);
            })
            : null;

        Logger = _loggerFactory?.CreateLogger("Waddle");

        if (cfg.Server is { } serverCfg)
        {
            Server = new WaddleServerContext(serverCfg, Logger, getPassword, _loggerFactory);
        }

        Config = cfg;
    }

    public void Dispose()
    {
        Logger?.LogDebug("Disposing of WaddleContext");
        Server?.Dispose();

        ClientOutputWriter.Flush();
        ClientOutput.Dispose();

        _loggerFactory?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Logger?.LogDebug("Disposing of WaddleContext asynchronously");
        if (Server is not null)
        {
            await Server.DisposeAsync();
        }

        await ClientOutputWriter.FlushAsync();
        await ClientOutput.DisposeAsync();

        _loggerFactory?.Dispose();
    }
}