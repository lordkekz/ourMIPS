using System.CommandLine;

namespace ourMIPSSharp_CLI;

class Program
{
    // TODO all of it
    static async Task<int> Main(string[] args)
    {
        var fileOptionC = new Option<FileInfo?>(
            new []{"-c", "-compile"},
            "The source file to compile.");
        
        var fileOptionO = new Option<FileInfo?>(
            new []{"-o", "-output"},
            "The output file write the bytecode to.");
        
        var fileOptionR = new Option<FileInfo?>(
            new []{"-", "-output"},
            "The output file write the bytecode to.");

        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddOption(fileOption);

        rootCommand.SetHandler((file) => 
            { 
                ReadFile(file!); 
            },
            fileOption);

        return await rootCommand.InvokeAsync(args);
    }

    static void ReadFile(FileInfo file)
    {
        File.ReadLines(file.FullName).ToList()
            .ForEach(line => Console.WriteLine(line));
    }
}