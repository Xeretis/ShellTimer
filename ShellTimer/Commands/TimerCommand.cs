using System.ComponentModel;
using System.Diagnostics;
using ShellTimer.Support.Cube;
using ShellTimer.Support.Enums;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ShellTimer.Commands;

internal sealed class TimerCommand : Command<TimerCommand.Settings>
{
    private const int ContentWidth = 80;
    private static TimerStatus Status { get; set; } = TimerStatus.Waiting;
    private static bool ExitRequested { get; set; }


    public override int Execute(CommandContext context, Settings settings)
    {
        while (Console.KeyAvailable)
            Console.ReadKey(true);

        var hasInspection = settings.InspectionTime > 0;
        var skipScrambleScreen = false;
        var nextScramble = ScrambleGenerator.GenerateScramble(settings.CubeSize, settings.ScrambleLength);

        while (true)
        {
            if (!skipScrambleScreen)
            {
                Status = TimerStatus.Waiting;
                ExitRequested = false;
                AnsiConsole.Clear();

                var scrambleTable = CreateScrambleTable(nextScramble, hasInspection);
                AnsiConsole.Write(scrambleTable);

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                    return 0;
                if (key.Key != ConsoleKey.Spacebar)
                    continue;
            }

            ExitRequested = false;
            AnsiConsole.Clear();

            Status = hasInspection ? TimerStatus.Inspection : TimerStatus.Started;

            var inspectionStopwatch = new Stopwatch();
            var stopwatch = new Stopwatch();

            using var cts = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(_ => ListenForKeyBoardEvent(cts.Token), null);

            if (hasInspection)
            {
                RunInspectionPhase(inspectionStopwatch, settings.InspectionTime);
                if (ExitRequested)
                {
                    cts.Cancel();
                    return 0;
                }
            }

            stopwatch.Start();
            var timerTable = CreateTimerTable(stopwatch);

            AnsiConsole.Live(timerTable).Start(ctx =>
            {
                while (Status != TimerStatus.Stopped && !ExitRequested)
                {
                    UpdateTimerTable(timerTable, stopwatch);
                    ctx.Refresh();
                    Thread.Sleep(100);
                }
            });

            stopwatch.Stop();
            cts.Cancel();

            if (ExitRequested)
                return 0;

            nextScramble = ScrambleGenerator.GenerateScramble(settings.CubeSize, settings.ScrambleLength);
            var resultTable = CreateResultTable(stopwatch.Elapsed, nextScramble);

            AnsiConsole.Clear();
            AnsiConsole.Write(resultTable);

            var key2 = Console.ReadKey(true);
            if (key2.Key == ConsoleKey.Escape)
                return 0;
            if (key2.Key != ConsoleKey.Spacebar)
                continue;

            skipScrambleScreen = true;
        }
    }

    private void RunInspectionPhase(Stopwatch inspectionStopwatch, int inspectionTime)
    {
        inspectionStopwatch.Start();
        var inspectionTable = CreateInspectionTable(inspectionStopwatch, inspectionTime);

        AnsiConsole.Live(inspectionTable).Start(ctx =>
        {
            while (Status != TimerStatus.Started &&
                   inspectionStopwatch.ElapsedMilliseconds <= inspectionTime * 1000 &&
                   !ExitRequested)
            {
                var inspectionGrid = CreateInspectionGrid(inspectionStopwatch, inspectionTime);
                inspectionTable.Rows.Update(0, 0, new Padder(inspectionGrid).PadTop(2).PadBottom(2));
                ctx.Refresh();
                Thread.Sleep(100);
            }
        });

        if (!ExitRequested)
            AnsiConsole.Clear();
        inspectionStopwatch.Stop();
    }

    private Table CreateScrambleTable(string scramble, bool hasInspection)
    {
        var table = new Table();
        table.AddColumn(new TableColumn("[blue]Scramble[/]").Centered());

        var grid = new Grid();
        grid.Width = ContentWidth;
        grid.AddColumn();
        grid.AddRow(new Markup($"[yellow]{scramble}[/]").Centered());

        var promptText = hasInspection
            ? "Press SPACE to start the inspection or ESC to exit..."
            : "Press SPACE to start the timer or ESC to exit...";
        grid.AddRow(new Markup($"[white]{promptText}[/]").Centered());

        table.AddRow(new Padder(grid).PadTop(2).PadBottom(2));
        table.Border = TableBorder.HeavyEdge;
        table.Width = ContentWidth;

        return table;
    }

    private Table CreateInspectionTable(Stopwatch stopwatch, int inspectionTime)
    {
        var inspectionTable = new Table();
        inspectionTable.AddColumn(new TableColumn("[green]Inspection[/]").Centered());
        inspectionTable.AddRow(new Padder(CreateInspectionGrid(stopwatch, inspectionTime)).PadTop(2).PadBottom(2));
        inspectionTable.Border = TableBorder.HeavyEdge;
        inspectionTable.Width = ContentWidth;
        return inspectionTable;
    }

    private Grid CreateInspectionGrid(Stopwatch stopwatch, int inspectionTime)
    {
        var grid = new Grid();
        grid.AddColumn();

        var progress = Math.Min(stopwatch.ElapsedMilliseconds / (inspectionTime * 1000.0), 1.0);
        grid.AddRow(new BreakdownChart().Width(ContentWidth)
            .AddItem("Inspection", 100 * progress, Color.Green)
            .AddItem("Remaining", 100 - 100 * progress, Color.Red)
            .HideTags()
            .HideTagValues());

        grid.AddRow(new Padder(new Markup($"{stopwatch.Elapsed}").Centered()).PadTop(1));
        grid.Expand = true;

        return grid;
    }

    private Table CreateTimerTable(Stopwatch stopwatch)
    {
        var table = new Table();
        table.AddColumn(new TableColumn("[green]Timer[/]").Centered());
        table.AddRow(new TableRow([new Padder(new Markup($"{stopwatch.Elapsed}")).PadTop(2).PadBottom(2)]));
        table.Border = TableBorder.HeavyEdge;
        table.Width = ContentWidth;
        return table;
    }

    private void UpdateTimerTable(Table table, Stopwatch stopwatch)
    {
        table.Rows.Update(0, 0, new Padder(new Markup($"{stopwatch.Elapsed}")).PadTop(2).PadBottom(2));
    }

    private Table CreateResultTable(TimeSpan elapsedTime, string nextScramble)
    {
        var table = new Table();
        table.AddColumn(new TableColumn("[blue]Results[/]").Centered());

        var grid = new Grid();
        grid.Width = ContentWidth;
        grid.AddColumn();

        grid.AddRow(new Markup($"[blue]{elapsedTime}[/]").Centered());
        grid.AddEmptyRow();
        grid.AddRow(new Markup("[yellow]Next scramble:[/]").Centered());
        grid.AddRow(new Markup($"[yellow]{nextScramble}[/]").Centered());
        grid.AddEmptyRow();
        grid.AddRow(new Markup("[white]Press SPACE to start again or ESC to exit[/]").Centered());

        table.AddRow(new Padder(grid).PadTop(2).PadBottom(2));
        table.Border = TableBorder.HeavyEdge;
        table.Width = ContentWidth;

        return table;
    }

    private static void ListenForKeyBoardEvent(CancellationToken cancellationToken)
    {
        do
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    Status = TimerStatus.Stopped;
                    ExitRequested = true;
                    return;
                }

                if (key.Key == ConsoleKey.Spacebar)
                {
                    if (Status == TimerStatus.Inspection)
                    {
                        Status = TimerStatus.Started;
                    }
                    else
                    {
                        Status = TimerStatus.Stopped;
                        return;
                    }
                }
            }

            Thread.Sleep(10);
        } while (true);
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-c|--cube-size")]
        [Description("Size of the Rubik's cube (2, 3, 4, etc.)")]
        public int CubeSize { get; set; } = 3;

        [CommandOption("-i|--inspection-time")]
        [Description("Inspection time in seconds (0 to disable)")]
        public int InspectionTime { get; set; } = 15;

        [CommandOption("-s|--scramble-length")]
        [Description("Length of the scramble in moves")]
        public int? ScrambleLength { get; set; } = null;
    }
}