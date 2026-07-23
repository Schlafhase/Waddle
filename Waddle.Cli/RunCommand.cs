using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Penguins;
using Penguins.ClientPenguins;
using Spectre.Console;
using Spectre.Console.Cli;
using Waddle.Config;
using Waddle.Config.Exceptions;
using YamlDotNet.Core;

namespace Waddle.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[workflow]")]
    [Description("The name of the file containing the workflow. The file ending can be omitted.")]
    [DefaultValue("")]
    public required string Workflow { get; init; }
}

public class RunCommand : AsyncCommand<RunSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        RunSettings settings,
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists("waddle.yaml"))
        {
            AnsiConsole.MarkupLine(
                "[red]Waddle hasn't been set up in this directory. Run `waddle init` to initialise a configuration file.[/]"
            );
            return 1;
        }

        WaddleConfig config;
        try
        {
            config = WaddleConfig.FromYaml(File.ReadAllText("waddle.yaml"));
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]The configuration is invalid:[/]");
            AnsiConsole.MarkupLine($"[red italic]{e.Message}[/]");
            if (e.InnerException is { } ie)
            {
                AnsiConsole.MarkupLine($"[red italic]  {ie.Message}[/]");
            }
            return 1;
        }

        // Set up Waddle Context
        await using WaddleContext waddleContext = new(
            config,
            getPassword: () =>
                AnsiConsole.Prompt(
                    new TextPrompt<string>(
                        "[yellow]Please enter your password for the remote host:[/]"
                    ).Secret()
                )
        );

        waddleContext.Logger?.LogInformation("Waddle context created. Hello World!");

        // Find workflow file

        // Parse workflow
        waddleContext.Logger?.LogInformation("Parsing workflow");
        List<YamlPenguin> workflow;

        string workflowName = !string.IsNullOrWhiteSpace(settings.Workflow)
            ? settings.Workflow
            : config.DefaultWorkflow;
        string sourceFile;

        try
        {
            workflow = WaddleWorkflow.FromWorkflowName(
                workflowName,
                out sourceFile,
                waddleContext.Logger
            );
        }
        catch (Exception e)
        {
            waddleContext.Logger?.LogCritical("Failed to parse workflow: {message}", e);
            if (config.VerboseErrors)
            {
                AnsiConsole.WriteException(e);
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]Failed to parse workflow: {e.Message}[/]"
                );
            }

            return 1;
        }

        RunWorkflowPenguin workflowPenguin;

        try
        {
            workflowPenguin = new(waddleContext, workflow, sourceFile)
            {
                Name = workflowName,
                State = PenguinState.Working,
            };
        }
        catch (StackOverflowException)
        {
            waddleContext.Logger?.LogCritical("Circular dependency in workflow");
            AnsiConsole.MarkupLine(
                "[red]Circular dependency in workflow detected. Recursive workflows are not supported.[/]"
            );
            return 1;
        }
        catch (MissingServerConfigException)
        {
            waddleContext.Logger?.LogCritical(
                "Missing server configuration in workflow that requires it"
            );
            AnsiConsole.MarkupLine(
                "[red]The workflow contains server penguins but your waddle configuration doesn't contain server information. Please add the [blue]Server[/] property to [blue]waddle.yaml[/] or fill out the required fields by running [blue]waddle init[/]. (You will have to add a server when asked)[/]"
            );
            return 1;
        }
        catch (Exception e)
        {
            waddleContext.Logger?.LogCritical("Failed to initialise workflow: {e}", e);
            AnsiConsole.MarkupLineInterpolated(
                $"[red]Failed to initialise workflow: {e.Message}[/]"
            );

            return 1;
        }

        // Run Workflow
        waddleContext.Logger?.LogInformation("Sarting workflow");
        try
        {
            await AnsiConsole
                .Live(renderWorkflow(workflowPenguin, waddleContext))
                .StartAsync(async ctx =>
                {
                    workflowPenguin.OnPenguinsChange = () =>
                        ctx.UpdateTarget(renderWorkflow(workflowPenguin, waddleContext));
                    await workflowPenguin.Execute(CancellationToken.None);
                });
        }
        catch (TaskCanceledException)
        {
            waddleContext.Logger?.LogError("A penguin with ingoreError set to false timed out.");
            AnsiConsole.MarkupLine("[red]A penguin timed out[/].");
            return 1;
        }
        catch (MissingServerConfigException)
        {
            AnsiConsole.MarkupLine(
                "[red]The workflow contains server penguins but your configuration doesn't support them. Please add server configuration to [blue]waddle.yaml[/] or add them using [blue]waddle init[/].[/]"
            );
            return 1;
        }
        catch (Exception e)
        {
            waddleContext.Logger?.LogError(
                "An error occurred in the workflow: \n{err}",
                e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace
            );
            if (config.VerboseErrors)
            {
                AnsiConsole.WriteException(e);
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
            }
            return 1;
        }

        waddleContext.Logger?.LogInformation("Exiting with code 0");
        return 0;
    }

    private static Tree renderWorkflow(RunWorkflowPenguin p, WaddleContext context)
    {
        WaddleConfig cfg = context.Config;
        bool error = p.Penguins.Any(p => p.State == PenguinState.Error);
        bool IgnoredError = p.IgnoreError && error;
        bool working = p.Penguins.Any(p => p.State == PenguinState.Working);
        bool finished = p.Penguins.All(p =>
            p.State is PenguinState.Success or PenguinState.IgnoredError
        );

        string masterColor;
        string masterSuffix;

        if (finished)
        {
            masterColor = "green";
            masterSuffix = cfg.SuccessIcon;
        }
        else if (IgnoredError)
        {
            masterColor = "purple";
            masterSuffix = cfg.IgnoredIcon;
        }
        else if (error)
        {
            masterColor = "red";
            masterSuffix = cfg.ErrorIcon;
        }
        else if (working)
        {
            masterColor = "yellow";
            masterSuffix = cfg.WaitingIcon;
        }
        else
        {
            masterColor = "dim";
            masterSuffix = cfg.IdleIcon;
        }

            masterSuffix += !string.IsNullOrWhiteSpace(p.Status) ? $"[dim]: {p.Status}[/]" : "";

        Tree t = new($"[{masterColor}]{Markup.Escape(p.Name)} {masterSuffix}[/]");

        foreach (IPenguin ip in p.Penguins)
        {
            if (ip is RunWorkflowPenguin rwp)
            {
                t.AddNode(renderWorkflow(rwp, context));
                continue;
            }
            string color = ip.State switch
            {
                PenguinState.Error => "red",
                PenguinState.IgnoredError => "purple",
                PenguinState.Working => "yellow",
                PenguinState.Success => "green",
                _ => "dim",
            };
            string suffix = ip.State switch
            {
                PenguinState.Error => cfg.ErrorIcon,
                PenguinState.IgnoredError => cfg.IgnoredIcon,
                PenguinState.Working => cfg.WaitingIcon,
                PenguinState.Success => cfg.SuccessIcon,
                _ => cfg.IdleIcon,
            };
            suffix += !string.IsNullOrWhiteSpace(ip.Status) ? $"[dim]: {ip.Status}[/]" : "";
            t.AddNode($"[{color}]{ip.Name} {suffix}[/]");
        }
        return t;
    }
}
