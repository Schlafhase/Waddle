using System.Reflection;
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
        WaddleConfig existingCfg = new()
        {
            Host = "",
            Username = "",
            DefaultWorkflow = "",
        };

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
        AnsiConsole.MarkupLine($"[dim]Waddle version {WaddleConfig.Version}[/]");

        AnsiConsole.MarkupLine(
            "[italic]Waddle needs some information to create a working configuration[/]"
        );
        AnsiConsole.WriteLine();

        WaddleConfig cfg = new()
        {
            Host = "",
            Username = "",
            DefaultWorkflow = "deploy",
        };

        // Hostname
        if (existing)
        {
            cfg.Host = AnsiConsole.Ask(
                "[green]Hostname[/][dim] of your nest (server):[/]",
                existingCfg.Host
            );
        }
        else
        {
            cfg.Host = AnsiConsole.Ask<string>("[green]Hostname[/][dim] of your nest (server):[/]");
        }

        // Username
        if (existing)
        {
            cfg.Username = AnsiConsole.Ask(
                "[dim]Your[/] [green]Username[/][dim] on your nest:[/]",
                existingCfg.Username
            );
        }
        else
        {
            cfg.Username = AnsiConsole.Ask<string>(
                "[dim]Your[/] [green]Username[/][dim] on your nest:[/]"
            );
        }

        // Port
        cfg.Port = AnsiConsole.Ask(
            "[green]SSH port[/] [dim]on your nest (probably 22):[/]",
            existing ? existingCfg.Port : 22
        );

        // Auth method
        SelectionPrompt<string> authPrompt = new SelectionPrompt<string>()
            .Title("[dim]What[/] [green]Authentication method[/] [dim]would you like to use?[/]")
            .AddChoices("Password", "Private key", "SSH Agent");

        if (existing)
        {
            if (existingCfg.UsePassword)
            {
                authPrompt = authPrompt.DefaultValue("Password");
            }
            else if (existingCfg.Keyfile is not null)
            {
                authPrompt = authPrompt.DefaultValue("Private key");
            }
            else if (existingCfg.UseSshAgent)
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
                cfg.UsePassword = true;
                AnsiConsole.MarkupLine(
                    "[italic]You will be prompted for the password when it's needed.[/]"
                );
                break;
            case "Private key":
                cfg.Keyfile = Path.GetFullPath(
                    AnsiConsole
                        .Ask(
                            "[dim]Path to the[/] [green]File containing the key[/][dim]:[/]",
                            "~/.ssh/id_ed25519"
                        )
                        .Replace("~", $"/home/{Environment.UserName}")
                );
                break;
            case "SSH Agent":
                cfg.UseSshAgent = true;
                break;
            default:
                throw new NotImplementedException("Invalid Authentication method");
        }

        // Server Output
        cfg.ServerOutputFileName = AnsiConsole
            .Prompt(
                new TextPrompt<string>(
                    "[dim]Filename to store[/] [green]Output from the server[/][dim] (leave empty to keep output in memory):[/]"
                )
                    .DefaultValue(existingCfg.ServerOutputFileName ?? "")
                    .AllowEmpty()
            )
            .Trim();

        if (string.IsNullOrWhiteSpace(cfg.ServerOutputFileName))
        {
            cfg.ServerOutputFileName = null;
        }
        else
        {
            cfg.ServerOutputFileName = Path.GetFullPath(cfg.ServerOutputFileName);
        }

        AnsiConsole.MarkupLineInterpolated(
            $"[green]Server output[/] [dim]goes to: [blue]{cfg.ServerOutputFileName ?? "Memory"}[/][/]"
        );

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

                        if (string.IsNullOrWhiteSpace(trimmed))
                        {
                            cfg.ClientOutputFileName = null;
                            return ValidationResult.Success();
                        }
                        else
                        {
                            string fullPath = Path.GetFullPath(trimmed);
                            return fullPath == cfg.ServerOutputFileName
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
        else
        {
            cfg.ClientOutputFileName = Path.GetFullPath(cfg.ClientOutputFileName);
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

        File.WriteAllText("waddle.yaml", cfg.ToYaml());

        AnsiConsole.MarkupLine("[green]Saved configuration to [blue]waddle.yaml.[/][/]");
        AnsiConsole.MarkupLine(
            "[dim]Run[/] [blue]waddle init[/] [dim]again or edit the file directly to change values.[/]"
        );

        return 0;
    }
}
