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
        ClientOutput = cfg.ClientOutputFileName is not null
            ? new FileStream(cfg.ClientOutputFileName, FileMode.Create)
            : new MemoryStream();
        ClientOutputWriter = new(ClientOutput);

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            if (cfg.LogFileName is not null)
            {
                builder.AddFile(cfg.LogFileName);
            }
            builder.AddFilter("Waddle", cfg.LogLevel);
        });

        if (cfg.Server is { } serverCfg)
        {
            Server = new WaddleServerContext(
                serverCfg,
                this,
                getPassword,
                cfg.LogFileName is not null ? _loggerFactory : null
            );
        }

        Logger = _loggerFactory.CreateLogger("Waddle");
    }

    public void Dispose()
    {
        Logger.LogDebug("Disposing of WaddleContext");
        Server?.Dispose();

        ClientOutputWriter.Flush();
        ClientOutput.Dispose();

        _loggerFactory.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Logger.LogDebug("Disposing of WaddleContext asynchronously");
        if (Server is not null)
        {
            await Server.DisposeAsync();
        }

        await ClientOutputWriter.FlushAsync();
        await ClientOutput.DisposeAsync();

        _loggerFactory.Dispose();
    }
}
