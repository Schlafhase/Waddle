using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Waddle.Config;

namespace Waddle.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[workflow]")]
    [Description("The name of the file containing the workflow. The file ending can be omitted.")]
    [DefaultValue("deploy")]
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

        string yaml;
        if (File.Exists(settings.Workflow))
        {
            yaml = File.ReadAllText(settings.Workflow);
        }
        else if (File.Exists(settings.Workflow + ".yaml"))
        {
            yaml = File.ReadAllText(settings.Workflow + ".yaml");
        }
        else if (File.Exists(settings.Workflow + ".yml"))
        {
            yaml = File.ReadAllText(settings.Workflow + ".yml");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]The requested workflow doesn't exist[/]");
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

        WaddleWorkflow workflow = WaddleWorkflow.FromYaml(yaml, settings.Workflow);

        await WorkflowRunner.Run(workflow, waddleContext);

        return 0;
    }
}
