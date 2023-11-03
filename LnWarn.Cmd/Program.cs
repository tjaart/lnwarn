using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace LnWarn.Cmd;

[Command(Description = "Count lines and return non zero when files exceed maximum number of lines.")]
class Program
{
    public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
        
    [Option(Description = "Lines that have less characters will not be counted.\nThis will ignore leading and trailing whitespace", Template = "--min-line-length|-ml")]
    [Range(1, 1000)]
    public int? MinLineLength { get; } = null;

    [Option(Description = "If any file has more than this maximum lines, then exit with a non zero exit code", Template = "--max|-m")]
    public int MaxLines { get; } = 50;

    [Option(Description = "A list of glob patterns for files to include", ShortName = "include", Template = "--include|-i")]
    public IEnumerable<string> IncludeGlobs { get; } = new[] { "**/*.cs" };
        
    [Option(Description = "A list of glob patterns for files to exclude", ShortName = "exclude", Template = "--exclude|-x")]
    public IEnumerable<string> ExcludeGlobs { get; } = new[] { "**/*.AssemblyInfo.cs" };

    [Option("--path|-p", Description = "Path to search for files", Template = "--path|-p")]
    public string Path { get; } = ".";

    [Option("--show-all|-a", Description = "Show all line counts above minimum")]
    public bool ShowAll { get; } = false;

    [UsedImplicitly]
    private async Task<int> OnExecute()
    {
        var filters = new List<LineFilterDelegates.ShouldLineBeIncluded>();

        Matcher matcher = new();
        foreach (var includeGlob in IncludeGlobs)
        {
            matcher.AddInclude(includeGlob);
        }
        
        foreach (var excludeGlob in ExcludeGlobs)
        {
            matcher.AddExclude(excludeGlob);
        }
            
        if (MinLineLength.TryGetValue(out var result))
        {
            filters.Add(inputString => inputString.Trim().Length >= result);
        }

        var resolvedPath = Path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        
        if (!System.IO.Path.Exists(resolvedPath))
        {
            //Console.WriteLine(Environment.GetFolderPath(Path));
            Console.WriteLine($"Path {resolvedPath} does not exist");
            return -1;
        }
        
        var directoryInfoWrapper = new DirectoryInfoWrapper(
            new DirectoryInfo(resolvedPath));
        var matchingFiles = matcher.Execute(
            directoryInfoWrapper);

        Console.WriteLine($"Analyzing directory \"{directoryInfoWrapper.FullName}\" ...");
        
        var lineCounter = new LineCounter(filters);

        var lineCountResults = new List<LineCountResult>();

        foreach (var matchingFilesFile in matchingFiles.Files)
        {
            await using var stream = File.OpenRead(System.IO.Path.Combine(resolvedPath,matchingFilesFile.Path) );
            var lineCount = await lineCounter.CountLines(stream);
            lineCountResults.Add(new LineCountResult(matchingFilesFile.Path, matchingFilesFile.Stem ?? "??", lineCount));
        }
            
        var ruleBreakers = lineCountResults.Where(c => c.LineCount > MaxLines).ToList();
        if (ruleBreakers.Any())
        {
            await WriteResult(ruleBreakers, lineCountResults);
            return ruleBreakers.Count;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed! No files exceed the maximum line count of {MaxLines}");
        return 0;
    }

    private async Task WriteResult(List<LineCountResult> ruleBreakers, List<LineCountResult> allLines)
    {
        
        await Console.Error.WriteLineAsync();
        DrawHorizontalLine();
        await Console.Error.WriteLineAsync(
            $"Count of lines with minimum length of {MinLineLength} exceeded the maximum number of allowed lines of {MaxLines} lines per file in the following {ruleBreakers.Count} file{(ruleBreakers.Count > 1 ? "s" : "")}");
        DrawHorizontalLine();
        await Console.Error.WriteLineAsync($"{"Path",-50}{"Lines",-10}");
        if (ShowAll)
        {
            foreach (var (line, stem, lineCount) in allLines.OrderByDescending(d=>d.LineCount))
            {
                if (ruleBreakers.Any(c=>c.Stem == stem))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                await Console.Error.WriteLineAsync($"{stem,-50}{lineCount,-10}");
                Console.ResetColor();
            }
        }
        else
        {
            foreach (var (_, stem, lineCount) in ruleBreakers.OrderByDescending(d=>d.LineCount))
            {
                if (ruleBreakers.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                await Console.Error.WriteLineAsync($"{stem,-50}{lineCount,-10}");
            }

        }
        Console.ResetColor();
        DrawHorizontalLine();
    }

    private void DrawHorizontalLine() => Console.Error.WriteLineAsync(new string('─', 150));
}