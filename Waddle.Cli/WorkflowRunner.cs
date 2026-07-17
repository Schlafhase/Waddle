using Microsoft.Extensions.Logging;
using Penguins;
using Penguins.ClientPenguins;
using Penguins.Exceptions;
using Penguins.ServerPenguins;
using Spectre.Console;
using Waddle.Config;

namespace Waddle.Cli;

public static class WorkflowRunner
{
    public static async Task Run(WaddleWorkflow workflow, WaddleContext context)
    {
        bool containsServerPenguins = false;
        WaddleServerContext getServerContextOrThrow()
        {
            containsServerPenguins = true;
            return context.Server ?? throw new MissingServerConfigException();
        }

        List<IPenguin> penguins = [];

        context.Logger.LogInformation("Converting raw WorkflowPenguins to IPenguin");
        foreach (WorkflowPenguin wp in workflow.WorkflowPenguins)
        {
            IPenguin p = wp switch
            {
                { Cmd: { } cmd } => new RunCommandPenguin(context)
                {
                    Name = wp.Name,
                    Command = cmd,
                },

                { ServerCmd: { } serverCmd } => new RunServerCommandPenguin(
                    context,
                    getServerContextOrThrow()
                )
                {
                    Name = wp.Name,
                    Command = serverCmd,
                },

                { ReceiveFolder: { } receiveFolder, Destination: { } destination } =>
                    new ReceiveFolderPenguin(context, getServerContextOrThrow())
                    {
                        Name = wp.Name,
                        Source = receiveFolder,
                        Destination = destination,
                    },

                { SendFolder: { } sendFolder, Destination: { } destination } =>
                    new SendFolderPenguin(context, getServerContextOrThrow())
                    {
                        Name = wp.Name,
                        Source = sendFolder,
                        Destination = destination,
                    },
                { SendFile: { } sendFile, Destination: { } destination } => new SendFilePenguin(
                    context,
                    getServerContextOrThrow()
                )
                {
                    Name = wp.Name,
                    Source = sendFile,
                    Destination = destination,
                },
                { ReceiveFile: { } receiveFile, Destination: { } destination } =>
                    new ReceiveFilePenguin(context, getServerContextOrThrow())
                    {
                        Name = wp.Name,
                        Source = receiveFile,
                        Destination = destination,
                    },
                _ => throw new ArgumentException("The workflow contains invalid penguins"),
            };
            context.Logger.LogDebug("Created IPenguin {name}", p.Name);
            p.TimeoutMs = wp.TimeoutMs;
            p.IgnoreError = wp.IgnoreError;
            penguins.Add(p);
        }

        if (containsServerPenguins)
        {
            await getServerContextOrThrow().Connect();
        }

        Dictionary<int, string> ignoredErrors = [];
        int error = -1;
        WaddleConfig cfg = context.Config;

        Tree getTree(int currentIndex)
        {
            context.Logger.LogDebug("Rebuilding CLI tree");
            bool finished = currentIndex == penguins.Count;
            string masterColor;
            string masterSuffix;

            if (finished)
            {
                masterColor = "green";
                masterSuffix = cfg.FinishedIcon;
            }
            else if (error >= 0 && currentIndex == error)
            {
                masterColor = "red";
                masterSuffix = cfg.ErrorIcon;
            }
            else
            {
                masterColor = "yellow";
                masterSuffix = cfg.WaitingIcon;
            }
            Tree t = new(
                $"[{masterColor}]{(finished || error >= 0 ? "" : " ")}{Markup.Escape(workflow.Name)} {masterSuffix}[/]"
            );
            for (int i = 0; i < penguins.Count; i++)
            {
                IPenguin p = penguins[i];
                string color;
                string suffix;
                if (error == i)
                {
                    color = "red";
                    suffix = cfg.ErrorIcon;
                }
                else if (ignoredErrors.TryGetValue(i, out string? error))
                {
                    color = "purple";
                    suffix = cfg.IgnoredIcon;
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        suffix += $"[dim]: {Markup.Escape(error)}[/]";
                    }
                }
                else if (currentIndex > i)
                {
                    color = "green";
                    suffix = cfg.FinishedIcon;
                }
                else if (currentIndex == i)
                {
                    color = "yellow";
                    suffix = cfg.WaitingIcon;
                    if (!string.IsNullOrWhiteSpace(context.Status))
                    {
                        suffix += $"[dim]: {Markup.Escape(context.Status)}[/]";
                    }
                }
                else
                {
                    color = "dim";
                    suffix = cfg.NotActiveIcon;
                }

                t.AddNode($"[{color}]{p.Name} {suffix}[/]");
            }
            return t;
        }
        Tree t = getTree(0);

        await AnsiConsole
            .Live(t)
            .StartAsync(async ctx =>
            {
                for (int i = 0; i < penguins.Count; i++)
                {
                    context.OnStatusChange = () => ctx.UpdateTarget(getTree(i));
                    IPenguin p = penguins[i];
                    using CancellationTokenSource tokenSource = p.TimeoutMs is { } timeout
                        ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout))
                        : new CancellationTokenSource();

                    try
                    {
                        context.Logger.LogInformation("Running {name}", p.Name);
                        await p.Execute(tokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        string message = e is TaskCanceledException or OperationCanceledException
                            ? "Penguin timed out."
                            : e.Message;
                        if (!p.IgnoreError)
                        {
                            error = i;
                            ctx.UpdateTarget(getTree(i));
                            throw;
                        }
                        context.Logger.LogWarning(
                            "Ignored error while running {name}: {err}",
                            p.Name,
                            e.Message
                        );
                        ignoredErrors.Add(i, message.Replace("\n", " "));
                        ctx.UpdateTarget(getTree(i + 1));
                    }

                    ctx.UpdateTarget(getTree(i + 1));
                }
                context.OnStatusChange = null;
            })
            .Spinner();
    }
}
