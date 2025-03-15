using System.ComponentModel;
using System.Diagnostics;
using ShellTimer.Support.Enums;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ShellTimer.Commands;

internal sealed class TimerCommand : Command<TimerCommand.Settings>
{
    public static TimerStatus Status { get; set; } = TimerStatus.Waiting;
    
    public sealed class Settings : CommandSettings
    {
   
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var stopwatch = new Stopwatch();
        
        AnsiConsole.MarkupLine("[yellow]Press any key to start the timer...[/]");
        
        Console.ReadKey(true);
        Status = TimerStatus.Started;
        Thread consoleKeyListener = new Thread(new ThreadStart(ListenForKeyBoardEvent));
        
        consoleKeyListener.Start();
        stopwatch.Start();

        AnsiConsole.Status().Start($"{stopwatch.Elapsed}", ctx =>
        {
            while (Status == TimerStatus.Started)
            {
                ctx.Status = $"{stopwatch.Elapsed}";
                Thread.Sleep(100);
            }
        });

        stopwatch.Stop();
        
        AnsiConsole.MarkupLine("[red]Timer stopped![/]");
        AnsiConsole.MarkupLine($"[blue]Elapsed time: {stopwatch.Elapsed}[/]");
        AnsiConsole.MarkupLine("[yellow]Press any key to exit...[/]");
        
        Console.ReadKey(true);
        return 0;
    }

    private static void ListenForKeyBoardEvent()
    {
        do
        {
            if (Console.ReadKey(true).Key == ConsoleKey.Spacebar)
            {
                Status = TimerStatus.Stopped;
            }
        } while (true); 
    }
}