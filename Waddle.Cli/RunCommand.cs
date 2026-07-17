using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Penguins.Exceptions;
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
                AnsiConsole.Ask<string>(
                    "[yellow]Please enter your password for the remote host:[/]"
                )
        );

        waddleContext.Logger?.LogInformation("Waddle context created. Hello World!");

        // Find workflow file
        List<string> allowedFileEndings = [".w.yaml", ".w.yml", ".yaml", ".yml"];
        string workflowName = settings.Workflow != "" ? settings.Workflow : config.DefaultWorkflow;
        bool hasFileEnding = allowedFileEndings.Any(workflowName.EndsWith);
        waddleContext.Logger?.LogTrace("Finding workflow file for `{workflow}`", workflowName);

        string yaml = "";
        if (hasFileEnding)
        {
            waddleContext.Logger?.LogTrace("Checking `{file}`", workflowName);
            if (File.Exists(workflowName))
            {
                waddleContext.Logger?.LogInformation(
                    "Using `{file}` as workflow file",
                    workflowName
                );
                yaml = File.ReadAllText(workflowName);
            }
        }
        else
        {
            foreach (string ending in allowedFileEndings)
            {
                waddleContext.Logger?.LogTrace("Checking `{file}`", workflowName + ending);
                if (File.Exists(workflowName + ending))
                {
                    waddleContext.Logger?.LogInformation(
                        "Using `{file}` as workflow file",
                        workflowName + ending
                    );
                    yaml = File.ReadAllText(workflowName + ending);
                    break;
                }
            }
        }
        if (string.IsNullOrWhiteSpace(yaml))
        {
            if (hasFileEnding)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]The requested workflow [blue]{workflowName}[/] is empty or doesn't exist. Create a file named [blue]{workflowName}[/] to create it.[/]"
                );
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]The requested workflow [blue]{workflowName}[/] is empty or doesn't exist. Create a file named [blue]{workflowName}.yaml[/] or [blue]{workflowName}.yml[/] to create it. You can also name the file [blue]{workflowName}.w.yaml[/] or [blue]{workflowName}.w.yml[/] to avoid conflicts with other tools.[/]"
                );
            }
            return 1;
        }

        // Parse workflow
        waddleContext.Logger?.LogInformation("Parsing workflow");
        WaddleWorkflow workflow;

        try
        {
            workflow = WaddleWorkflow.FromYaml(yaml, workflowName);
            ArgumentNullException.ThrowIfNull(workflow.WorkflowPenguins);
        }
        catch (Exception e)
        {
            waddleContext.Logger?.LogCritical("Failed to parse workflow: {message}", e.Message);
            throw;
        }

        // Run Workflow
        waddleContext.Logger?.LogInformation("Sarting workflow");
        try
        {
            await WorkflowRunner.Run(workflow, waddleContext);
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
}
