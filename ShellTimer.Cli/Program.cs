// See https://aka.ms/new-console-template for more information

using ShellTimer.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<TimerCommand>("timer")
        .WithDescription("Start timing solves");

    config.AddCommand<ClearCommand>("clear")
        .WithDescription("Clear all solve records from the database");

    config.AddCommand<ListCommand>("list")
        .WithDescription("List all solve records");

    config.AddCommand<StatsCommand>("stats")
        .WithDescription("Show statistics for a specific cube size");

    config.AddCommand<DeleteCommand>("delete")
        .WithDescription("Delete a solve record by its ID");
});

return app.Run(args);