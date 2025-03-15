// See https://aka.ms/new-console-template for more information

using ShellTimer.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<TimerCommand>("timer")
        .WithDescription("Start timing solves");

    config.AddCommand<ClearCommand>("clear")
        .WithDescription("Clear all solve records from the database");
});

return app.Run(args);