using System.Data;
using Penguins;
using Penguins.ClientPenguins;
using Penguins.ServerPenguins;
using Spectre.Console;
using Waddle.Config;

namespace Waddle.Cli;

public static class WorkflowRunner
{
    public static async Task Run(WaddleWorkflow workflow, WaddleContext context)
    {
        List<IPenguin> penguins = [];

        foreach (WorkflowPenguin wp in workflow.WorkflowPenguins)
        {
            IPenguin p = wp switch
            {
                { Cmd: { } cmd } => new RunCommandPenguin(context)
                {
                    Command = cmd,
                    Name = wp.Name,
                    IgnoreError = wp.IgnoreError,
                    TimeoutMs = wp.TimeoutMs,
                },

                { ServerCmd: { } serverCmd } => new RunServerCommandPenguin(context)
                {
                    Command = serverCmd,
                    Name = wp.Name,
                    IgnoreError = wp.IgnoreError,
                    TimeoutMs = wp.TimeoutMs,
                },

                { GetFolder: { } getFolder } => throw new NotImplementedException(),

                { SendFolder: { } sendFolder } => throw new NotImplementedException(),

                _ => throw new ArgumentException(
                    "Workflow contains invalid penguins",
                    nameof(workflow)
                ),
            };
            penguins.Add(p);
        }

        Dictionary<int, string> ignoredErrors = [];
        int error = -1;
        WaddleConfig cfg = context.Config;

        Tree getTree(int currentIndex)
        {
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
                        suffix += $"[dim]: {error}[/]";
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
                    IPenguin p = penguins[i];
                    using CancellationTokenSource tokenSource = p.TimeoutMs is { } timeout
                        ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout))
                        : new CancellationTokenSource();

                    try
                    {
                        await p.Execute(tokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        string message =
                            e is TaskCanceledException ? "Penguin timed out." : e.Message;
                        if (!p.IgnoreError)
                        {
                            error = i;
                            ctx.UpdateTarget(getTree(i));
                            throw;
                        }
                        ignoredErrors.Add(i, message.Replace("\n", " "));
                        ctx.UpdateTarget(getTree(i + 1));
                    }

                    ctx.UpdateTarget(getTree(i + 1));
                }
            })
            .Spinner();
    }
}
