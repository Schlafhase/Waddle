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

        context.Logger?.LogInformation("Converting raw WorkflowPenguins to IPenguin");
        foreach (WorkflowPenguin wp in workflow.WorkflowPenguins)
        {
            IPenguin p = wp switch
            {
                { Cmd: { } cmd } => new RunCommandPenguin(context)
                {
                    Name = wp.Name,
                    Command = cmd,
                    Shell = wp.Shell,
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
            context.Logger?.LogDebug("Created IPenguin {name}", p.Name);
            p.TimeoutMs = wp.TimeoutMs;
            p.IgnoreError = wp.IgnoreError;
            p.State = PenguinState.Idle;

            penguins.Add(p);
        }

        if (containsServerPenguins)
        {
            await getServerContextOrThrow().Connect();
        }

        WaddleConfig cfg = context.Config;

        Tree getUI()
        {
            bool finished = penguins.All(p =>
                p.State is PenguinState.Success or PenguinState.IgnoredError
            );
            bool error = penguins.Any(p => p.State == PenguinState.Error);

            string masterColor;
            string masterSuffix;

            if (finished)
            {
                masterColor = "green";
                masterSuffix = cfg.FinishedIcon;
            }
            else if (error)
            {
                masterColor = "red";
                masterSuffix = cfg.ErrorIcon;
            }
            else
            {
                masterColor = "yellow";
                masterSuffix = cfg.WaitingIcon;
            }
            Tree t = new($"[{masterColor}]{Markup.Escape(workflow.Name)} {masterSuffix}[/]");
            for (int i = 0; i < penguins.Count; i++)
            {
                IPenguin p = penguins[i];
                string color;
                string suffix;
                if (p.State == PenguinState.Error)
                {
                    color = "red";
                    suffix = cfg.ErrorIcon;
                }
                else if (p.State == PenguinState.IgnoredError)
                {
                    color = "purple";
                    suffix = cfg.IgnoredIcon;
                    if (!string.IsNullOrWhiteSpace(p.Status))
                    {
                        suffix += $"[dim]: {Markup.Escape(p.Status)}[/]";
                    }
                }
                else if (p.State == PenguinState.Success)
                {
                    color = "green";
                    suffix = cfg.FinishedIcon;
                }
                else if (p.State == PenguinState.Working)
                {
                    color = "yellow";
                    suffix = cfg.WaitingIcon;
                    if (!string.IsNullOrWhiteSpace(p.Status))
                    {
                        suffix += $"[dim]: {Markup.Escape(p.Status)}[/]";
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
        Tree ui = getUI();

        await AnsiConsole
            .Live(ui)
            .StartAsync(async ctx =>
            {
                for (int i = 0; i < penguins.Count; i++)
                {
                    context.Logger?.LogTrace("Entering new workflow penguin");
                    IPenguin p = penguins[i];
                    p.OnStatusChange = () => ctx.UpdateTarget(getUI());

                    p.State = PenguinState.Working;
                    ctx.UpdateTarget(getUI());

                    using CancellationTokenSource tokenSource = p.TimeoutMs is { } timeout
                        ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout))
                        : new CancellationTokenSource();

                    try
                    {
                        context.Logger?.LogInformation("Running {name}", p.Name);
                        await p.Execute(tokenSource.Token);
                        p.State = PenguinState.Success;
                        p.Status = "";
                    }
                    catch (Exception e)
                    {
                        string message = e is TaskCanceledException or OperationCanceledException
                            ? "Penguin timed out."
                            : e.Message;
                        if (!p.IgnoreError)
                        {
                            p.State = PenguinState.Error;
                            ctx.UpdateTarget(getUI());
                            throw;
                        }
                        context.Logger?.LogWarning(
                            "Ignored error while running {name}: {err}",
                            p.Name,
                            e.Message
                        );
                        p.State = PenguinState.IgnoredError;
                        p.Status = message.Replace("\n", " ");
                        ctx.UpdateTarget(getUI());
                    }

                    p.OnStatusChange = null;
                    ctx.UpdateTarget(getUI());
                }
            });
    }
}