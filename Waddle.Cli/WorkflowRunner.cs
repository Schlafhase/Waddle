using Penguins;
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
                { Cmd: { } cmd, ServerCmd: null, GetFolder: null, SendFolder: null } =>
                    throw new NotImplementedException(),

                { Cmd: null, ServerCmd: { } serverCmd, GetFolder: null, SendFolder: null } =>
                    new RunServerCommand(context)
                    {
                        Command = serverCmd,
                        Name = wp.Name,
                        IgnoreError = wp.IgnoreError,
                    },

                { Cmd: null, ServerCmd: null, GetFolder: { } getFolder, SendFolder: null } =>
                    throw new NotImplementedException(),

                { Cmd: null, ServerCmd: null, GetFolder: null, SendFolder: { } sendFolder } =>
                    throw new NotImplementedException(),

                _ => throw new ArgumentException(
                    "Workflow contains invalid penguins",
                    nameof(workflow)
                ),
            };
            penguins.Add(p);
        }

        HashSet<int> ignoredErrors = [];
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
                else if (ignoredErrors.Contains(i))
                {
                    color = "purple";
                    suffix = cfg.IgnoredIcon;
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
                    try
                    {
                        switch (penguins[i])
                        {
                            case SynchronousPenguin sp:
                                sp.Execute();
                                break;

                            case AsyncPenguin ap:
                                await ap.Execute();
                                break;

                            default:
                                throw new InvalidOperationException(
                                    "Penguin was neither AsyncPenguin nor SynchronousPenguin"
                                );
                        }
                    }
                    catch (Exception)
                    {
                        if (!penguins[i].IgnoreError)
                        {
                            error = i;
                            ctx.UpdateTarget(getTree(i));
                            throw;
                        }
                        ignoredErrors.Add(i);
                        ctx.UpdateTarget(getTree(i+1));
                    }

                    context.CancellationToken = new CancellationTokenSource(
                        TimeSpan.FromMinutes(5)
                    ).Token;
                    ctx.UpdateTarget(getTree(i + 1));
                }
            })
            .Spinner();
    }
}
