using System;
using System.Collections.Generic;
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
        
    [Option(Description = "Lines that have less characters will not be counted.\nThis will ignore initial whitespace", Template = "--min-line-length|-ml")]
    [Range(1, 1000)]
    public int? MinLineLength { get; } = null;

    [Option(Description = "If any file has more than this maximum lines, then exit with a non zero exit code", Template = "--max|-m")]
    public int MaxLines { get; } = 5;

    [Option(Description = "A list of glob patterns for files to include", ShortName = "include", Template = "--include|-i")]
    public IEnumerable<string> IncludeGlobs { get; } = new[] { "**/*.cs" };
        
    [Option(Description = "A list of glob patterns for files to exclude", ShortName = "exclude", Template = "--exclude|-x")]
    public IEnumerable<string> ExcludeGlobs { get; } = new[] { "**/*.AssemblyInfo.cs" };

    [UsedImplicitly]
    private async Task<int> OnExecute()
    {
        var filters = new List<LineFilterDelegates.ShouldLineBeIncluded>();

        Matcher matcher = new();
        foreach (var includeGlob in IncludeGlobs)
        {
            matcher.AddInclude(includeGlob);
        }
            
        if (MinLineLength.TryGetValue(out var result))
        {
            filters.Add(inputString => inputString.Length >= result);
        }
            
        string searchDirectory = ".";

        var directoryInfoWrapper = new DirectoryInfoWrapper(
            new DirectoryInfo(searchDirectory));
        PatternMatchingResult matchingFiles = matcher.Execute(
            directoryInfoWrapper);

        Console.WriteLine($"Analyzing directory \"{directoryInfoWrapper.FullName}\" ...");
        
        var lineCounter = new LineCounter(filters);

        var lineCountResults = new List<LineCountResult>();

        foreach (var matchingFilesFile in matchingFiles.Files)
        {
            await using var stream = File.OpenRead(matchingFilesFile.Path);
            var lineCount = await lineCounter.CountLines(stream);
            lineCountResults.Add(new LineCountResult(matchingFilesFile.Path, matchingFilesFile.Stem ?? "??", lineCount));
        }
            
        var ruleBreakers = lineCountResults.Where(c => c.LineCount > MaxLines).ToList();
        if (ruleBreakers.Any())
        {
            await WriteResult(ruleBreakers);
            return 1;
        }
            
        return 0;
    }

    private async Task WriteResult(List<LineCountResult> ruleBreakers)
    {
        if (ruleBreakers.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        await Console.Error.WriteLineAsync();
        DrawHorizontalLine();
        await Console.Error.WriteLineAsync(
            $"Count of lines with minimum length of {MinLineLength} exceeded the maximum number of allowed lines {MaxLines} per file in the following {ruleBreakers.Count} file{(ruleBreakers.Count > 1 ? "s" : "")}");
        DrawHorizontalLine();
        await Console.Error.WriteLineAsync($"{"Path",-50}{"Lines",-10}");
        foreach (var (_, stem, lineCount) in ruleBreakers.OrderByDescending(d=>d.LineCount))
        {
            await Console.Error.WriteLineAsync($"{stem,-50}{lineCount,-10}");
        }

        DrawHorizontalLine();
        Console.ResetColor();
    }

    private void DrawHorizontalLine() => Console.Error.WriteLineAsync(new string('─', 150));
}