using System.Reflection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Waddle.Config;

namespace Waddle.Cli;

public class InitCommand : Command
{
    protected override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        bool existing = false;
        // Set required fields so the compiler doesn't complain
        WaddleConfig existingCfg = new() { DefaultWorkflow = "" };

        if (File.Exists("waddle.yaml"))
        {
            AnsiConsole.MarkupLine("[yellow]A waddle configuration exists in this directory.[/]");
            try
            {
                existingCfg = WaddleConfig.FromYaml(File.ReadAllText("waddle.yaml"));
                existing = true;

                Table configTable = new();
                configTable.AddColumns("Key", "Value");
                foreach (FieldInfo info in typeof(WaddleConfig).GetFields())
                {
                    configTable.AddRow(
                        info.Name,
                        info.GetValue(existingCfg)?.ToString() ?? "[italic]null[/]"
                    );
                }

                AnsiConsole.Write(configTable);
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine("[red]The existing configuration is invalid:[/]");
                AnsiConsole.MarkupLine($"[red italic]{e.Message}[/]");
                if (e.InnerException is { } ie)
                {
                    AnsiConsole.MarkupLine($"[red italic]  {ie.Message}[/]");
                }
            }

            if (
                !AnsiConsole.Confirm(
                    "[dim]Do you want to override the existing configuration?[/]",
                    false
                )
            )
            {
                return 0;
            }
        }

        FigletFont font;
        try
        {
            font = FigletFont.Load(
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("Waddle.Cli.Resources.colossal.flf")
                    ?? throw new InvalidOperationException(
                        "The assembly doesn't contain the required font 'colossal.flf'"
                    )
            );
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
            return 1;
        }

        FigletText title = new FigletText(font, "Waddle").Color(Color.Yellow);
        AnsiConsole.Write(title);
        AnsiConsole.MarkupLine($"[dim]Waddle version {WaddleContext.VersionString}[/]");

        AnsiConsole.MarkupLine(
            "[italic]Waddle needs some information to create a working configuration[/]"
        );
        AnsiConsole.WriteLine();

        WaddleConfig cfg = new() { DefaultWorkflow = "deploy" };

        if (!AnsiConsole.Confirm("[dim]Do you want to [blue]connect a nest[/] (server)?[/]", true))
        {
            goto client;
        }

        WaddleServerConfig serverConfig = new() { Host = "", Username = "" };

        // Hostname
        serverConfig.Host = ask(
            "[green]Hostname[/][dim] of your nest:[/]",
            existingCfg.Server?.Host
        );

        // Username
        serverConfig.Username = ask(
            "[dim]Your[/] [green]Username[/][dim] on your nest:[/]",
            existingCfg.Server?.Username
        );

        // Port
        serverConfig.Port = ask(
            "[green]SSH port[/] [dim]on your nest (probably 22):[/]",
            existing ? (existingCfg.Server?.Port ?? 22) : 22
        );

        // Auth method
        SelectionPrompt<string> authPrompt = new SelectionPrompt<string>()
            .Title("[dim]What[/] [green]Authentication method[/] [dim]would you like to use?[/]")
            .AddChoices("Password", "Private key", "SSH Agent");

        if (existing && existingCfg.Server is { } existingServerConfig)
        {
            if (existingServerConfig.UsePassword)
            {
                authPrompt = authPrompt.DefaultValue("Password");
            }
            else if (existingServerConfig.Keyfile is not null)
            {
                authPrompt = authPrompt.DefaultValue("Private key");
            }
            else if (existingServerConfig.UseSshAgent)
            {
                authPrompt = authPrompt.DefaultValue("SSH Agent");
            }
        }

        string authMethod = AnsiConsole.Prompt(authPrompt);

        AnsiConsole.MarkupLineInterpolated(
            $"[green]Authentication method[/][dim]: {authMethod}[/]"
        );

        switch (authMethod)
        {
            case "Password":
                serverConfig.UsePassword = true;
                AnsiConsole.MarkupLine(
                    "[italic]You will be prompted for the password when it's needed.[/]"
                );
                break;
            case "Private key":
                serverConfig.Keyfile = AnsiConsole.Ask(
                    "[dim]Path to the[/] [green]File containing the key[/][dim]:[/]",
                    "~/.ssh/id_ed25519"
                );
                break;
            case "SSH Agent":
                serverConfig.UseSshAgent = true;
                break;
            default:
                throw new NotImplementedException("Invalid Authentication method");
        }

        // Server Output
        serverConfig.ServerOutputFileName = AnsiConsole
            .Prompt(
                prompt(
                        "[dim]Filename to store[/] [green]Output from the server[/][dim] (leave empty to keep output in memory):[/]",
                        existingCfg.Server?.ServerOutputFileName
                    )
                    .AllowEmpty()
            )
            .Trim();

        if (string.IsNullOrWhiteSpace(serverConfig.ServerOutputFileName))
        {
            serverConfig.ServerOutputFileName = null;
        }

        AnsiConsole.MarkupLineInterpolated(
            $"[green]Server output[/] [dim]goes to: [blue]{serverConfig.ServerOutputFileName ?? "Memory"}[/][/]"
        );

        cfg.Server = serverConfig;

        client:
        // Client output
        cfg.ClientOutputFileName = AnsiConsole
            .Prompt(
                new TextPrompt<string>(
                    "[dim]Filename to store[/] [green]Output from the client[/][dim] (leave empty to keep output in memory):[/]"
                )
                    .DefaultValue(existingCfg.ClientOutputFileName ?? "")
                    .AllowEmpty()
                    .Validate(input =>
                    {
                        string trimmed = input.Trim();

                        if (
                            cfg.Server?.ServerOutputFileName is null
                            || string.IsNullOrWhiteSpace(trimmed)
                        )
                        {
                            cfg.ClientOutputFileName = null;
                            return ValidationResult.Success();
                        }
                        else
                        {
                            string fullPath = Path.GetFullPath(trimmed);
                            return fullPath == Path.GetFullPath(((WaddleServerConfig)cfg.Server).ServerOutputFileName)
                                ? ValidationResult.Error(
                                    "Client output can't be the same as server output."
                                )
                                : ValidationResult.Success();
                        }
                    })
            )
            .Trim();

        if (string.IsNullOrWhiteSpace(cfg.ClientOutputFileName))
        {
            cfg.ClientOutputFileName = null;
        }

        AnsiConsole.MarkupLineInterpolated(
            $"[green]Client output[/] [dim]goes to: [blue]{cfg.ClientOutputFileName ?? "Memory"}[/][/]"
        );

        // Default workflow
        cfg.DefaultWorkflow = AnsiConsole.Ask(
            "[dim]Name of the[/] [green]default workflow[/] [dim](run [blue]waddle[/] without arguments to run the default workflow):[/]",
            "deploy"
        );

        // Nerd Fonts
        if (
            AnsiConsole.Confirm(
                "[dim]Do you want to use[/] [green]Nerd Fonts[/][dim] for icons? (requires a Nerd Font to be installed)[/]",
                existingCfg.FinishedIcon == "󰗠 [italic dim]Success[/]"
            )
        )
        {
            cfg.FinishedIcon = "󰗠 [italic dim]Success[/]";
            // cfg.WaitingIcon = " ";
            cfg.WaitingIcon = "󰔚 [italic dim]Working[/]";
            cfg.ErrorIcon = "󰅙 [italic dim]Error[/]";
            cfg.NotActiveIcon = " ";
            cfg.IgnoredIcon = " [italic dim]Ignored Error[/]";
        }
        else
        {
            cfg.FinishedIcon = ":check_mark: [italic dim]Success[/]";
            cfg.WaitingIcon = ":three_o_clock: [italic dim]Working[/]";
            cfg.ErrorIcon = "× [italic dim]Error[/]";
            cfg.NotActiveIcon = ":zzz:";
            cfg.IgnoredIcon = ":minus: [italic dim]Ignored Error[/]";
        }

        cfg.LogLevel = LogLevel.Information;

        File.WriteAllText("waddle.yaml", cfg.ToYaml());

        AnsiConsole.MarkupLine("[green]Saved configuration to [blue]waddle.yaml.[/][/]");
        AnsiConsole.MarkupLine(
            "[dim]Run[/] [blue]waddle init[/] [dim]again or edit the file directly to change values.[/]"
        );

        return 0;
    }

    private static T ask<T>(string prompt, T? defaultValue)
    {
        if (defaultValue is { } defaultValueNotNull)
        {
            return AnsiConsole.Ask(prompt, defaultValueNotNull);
        }

        return AnsiConsole.Ask<T>(prompt);
    }

    private static TextPrompt<T> prompt<T>(string prompt, T? defaultValue)
    {
        TextPrompt<T> p = new TextPrompt<T>(prompt);
        return defaultValue is { } defaultValueNotNull ? p.DefaultValue(defaultValueNotNull) : p;
    }
}
