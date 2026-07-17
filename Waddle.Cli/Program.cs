using Spectre.Console.Cli;
using Waddle.Cli;

CommandApp<RunCommand> app = new();


app.Configure(config => config.AddCommand<InitCommand>("init"));

return app.Run(args);
