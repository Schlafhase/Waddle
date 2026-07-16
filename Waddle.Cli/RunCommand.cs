using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Waddle.Config;

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
            AnsiConsole.Markup(
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

        string workflowName = settings.Workflow != "" ? settings.Workflow : config.DefaultWorkflow;
        bool hasFileEnding = workflowName.EndsWith(".yaml") || workflowName.EndsWith(".yml");

        string yaml;
        if (File.Exists(workflowName) && hasFileEnding)
        {
            yaml = File.ReadAllText(workflowName);
        }
        else if (File.Exists(workflowName + ".yaml") && !hasFileEnding)
        {
            yaml = File.ReadAllText(workflowName + ".yaml");
        }
        else if (File.Exists(workflowName + ".yml") && !hasFileEnding)
        {
            yaml = File.ReadAllText(workflowName + ".yml");
        }
        else
        {
            if (hasFileEnding)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]The requested workflow [blue]{workflowName}[/] doesn't exist. Create a file named [blue]{workflowName}[/] to create it.[/]"
                );
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]The requested workflow [blue]{workflowName}[/] doesn't exist. Create a file named [blue]{workflowName}.yaml[/] or [blue]{workflowName}.yml[/] to create it.[/]"
                );
            }
            return 1;
        }

        using WaddleContext waddleContext = new(
            config,
            getPassword: () =>
                AnsiConsole.Ask<string>(
                    "[yellow]Please enter your password for the remote host:[/]"
                )
        );

        await AnsiConsole
            .Status()
            .StartAsync("[yellow]Connecting[/]", async _ => await waddleContext.Initialise());
        AnsiConsole.MarkupLine($"[green]Connected {config.FinishedIcon}[/]");

        WaddleWorkflow workflow = WaddleWorkflow.FromYaml(yaml, workflowName);

        try
        {
            await WorkflowRunner.Run(workflow, waddleContext);
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[red]A penguin timed out[/].");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
        }

        return 0;
    }
}
