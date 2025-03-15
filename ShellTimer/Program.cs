// See https://aka.ms/new-console-template for more information

using ShellTimer.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<TimerCommand>("timer");
});

return app.Run(args);