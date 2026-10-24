using System;
using DeltaAssetEditor.Cli;
using DeltaAssetEditor.Core.Services;

namespace DeltaAssetEditor;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=================================================");
        Console.WriteLine("       Deltarune Data.win Modding Tool (.NET 8)  ");
        Console.WriteLine("=================================================\n");

        IProjectService projectService = new ProjectService();
        CommandLineHandler handler = new CommandLineHandler(projectService);

        return handler.Run(args);
    }
}
