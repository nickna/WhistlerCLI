using System;
using System.Collections.Generic;
using System.Reflection;
using Spectre.Console;
using WhistlerCLI;

internal class Program
{
    private const string CMD_LIST = "list";
    private const string CMD_RENID = "renid";
    private const string CMD_REN = "ren";

    private readonly IWslUtil _wslUtil;

    public Program(IWslUtil wslUtil)
    {
        _wslUtil = wslUtil;
    }

    public static void Main(string[] args)
    {
        var program = new Program(new WslUtil());
        program.Run(args);
    }

    public void Run(string[] args)
    {
        DisplayProgramInfo();

        if (!OperatingSystem.IsWindows())
        {
            AnsiConsole.MarkupLine("[underline red]Warning:[/] This program is designed for Windows.\n");
            return;
        }

        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("No command provided. Available commands are 'list', 'renid', and 'ren'.");
            return;
        }

        var command = args[0].ToLower();
        var commandHandlers = new Dictionary<string, Action<string[]>>
        {
            { CMD_LIST, _ => ListDistros() },
            { CMD_RENID, args => RenameById(args) },
            { CMD_REN, args => Rename(args) }
        };

        if (commandHandlers.TryGetValue(command, out var handler))
        {
            handler(args);
        }
        else
        {
            AnsiConsole.MarkupLine($"Unknown command '{command}'. Available commands are 'list', 'renid', and 'ren'.");
        }
    }

    private void DisplayProgramInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        var projectUrl = "https://github.com/nickna/WhistlerCLI";

        AnsiConsole.Markup($"[bold]{title}[/] ");
        AnsiConsole.MarkupLine($"[grey]-- {description}[/]");
        AnsiConsole.MarkupLine($"[grey]Ver:[/] [bold]{version}[/]");
        AnsiConsole.MarkupLine($"[grey]{copyright} -- BSD license[/]");
        AnsiConsole.MarkupLine($"[grey]Project URL:[/] [bold]{projectUrl}[/]\n");
    }

    private void ListDistros()
    {
        var distros = _wslUtil.WslDistros;

        if (distros.Count == 0)
        {
            AnsiConsole.MarkupLine("No WSL distributions found.");
            return;
        }

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Space");
        table.AddColumn("LastAccess");
        table.Border(TableBorder.None);

        foreach (var distro in distros)
        {
            string distroName = distro.Default ? $"[bold yellow]{distro.DistroName}[/]" : distro.DistroName;
            table.AddRow(distro.Id.ToString(), distroName, distro.TotalSpace, distro.LastAccessStr);
        }
        AnsiConsole.Write(table);
    }

    private void RenameById(string[] args)
    {
        if (args.Length < 3)
        {
            AnsiConsole.MarkupLine("Usage: renid <Id> <newName>");
            return;
        }

        if (!int.TryParse(args[1], out int id))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid Id '{args[1]}'.");
            return;
        }

        var distros = _wslUtil.WslDistros;
        var distro = distros.Find(x => x.Id == id);

        if (distro.Equals(default(WslDistro)))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Id {id} not found.");
            return;
        }

        RenameDistro(distro.DistroName, args[2]);
    }

    private void Rename(string[] args)
    {
        if (args.Length < 3)
        {
            AnsiConsole.MarkupLine("Usage: ren <oldName> <newName>");
            return;
        }

        RenameDistro(args[1], args[2]);
    }

    private void RenameDistro(string oldName, string newName)
    {
        var success = _wslUtil.UpdateWSLDistroName(oldName, newName);
        if (success)
        {
            AnsiConsole.MarkupLine($"Success: Renamed {oldName} to {newName}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to rename {oldName} to {newName}");
        }
    }
}

public interface IWslUtil
{
    List<WslDistro> WslDistros { get; }
    bool UpdateWSLDistroName(string distroName, string newName);
}

