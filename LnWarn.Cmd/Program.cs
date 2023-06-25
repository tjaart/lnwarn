using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace LnWarn.Cmd
{
    [Command(Description = "My global command line tool.")]
    class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
        
        [Option(Description = "Lines that have less characters will not be counted.\nThis will ignore initial whitespace", Template = "--min-line-length|-ml")]
        [Range(1, 1000)]
        public int? MinLineLength { get; } = null;

        [Option(Description = "If any file has more than this maximum lines, then exit with a non zero exit code", Template = "--max|-m")]
        public int MaxLines { get; } = 5;

        [Option(Description = "", ShortName = "include", Template = "--include|-i")]
        public IEnumerable<string> IncludeGlobs { get; } = new[] { "**/*.cs" };

        [UsedImplicitly]
        private async Task<int> OnExecute()
        {
            var filters = new List<LineFilterDelegates.ShouldLineBeIncluded>();

            Matcher matcher = new();
            foreach (var includeGlob in IncludeGlobs)
            {
                matcher.AddExclude(includeGlob);
            }
            
            if (MinLineLength.TryGetValue(out var result))
            {
                filters.Add(inputString => inputString.Length >= result);
            }
            
            string searchDirectory = ".";

            PatternMatchingResult matchingFiles = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(searchDirectory)));

            var lineCounter = new LineCounter(filters);

            var lineCountResults = new List<LineCountResult>();
            
            foreach (var matchingFilesFile in matchingFiles.Files)
            {
                await using var stream = File.OpenRead(matchingFilesFile.Path);
                var lineCount = await lineCounter.CountLines(stream);
                lineCountResults.Add(new LineCountResult(matchingFilesFile.Path, matchingFilesFile.Stem, lineCount));
            }

            var ruleBreakers = lineCountResults.Where(c => c.LineCount > MaxLines).ToList();
            if (ruleBreakers.Any())
            {
                foreach (var (path, stem, lineCount) in ruleBreakers)
                {
                    await Console.Error.WriteLineAsync($"{stem,-50}{lineCount,-10}");
                }

                return 1;
            }
            
            return 0;
        }
    }

    internal record LineCountResult(string Path, string Stem, uint LineCount)
    {
    }
}

public static class NullableExtensions
{
    public static bool TryGetValue<T>(this T? anything,[NotNullIfNotNull(nameof(anything))] out T result) 
    {
        if (anything is not null)
        {
            result = anything;
            return true;
        }

        result = default;
        return false;
    }
    }