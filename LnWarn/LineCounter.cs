namespace LnWarn;

public class LineCounter
{
    private readonly List<LineFilterDelegates.ShouldLineBeIncluded> _lineIncludeFilters;

    public LineCounter(List<LineFilterDelegates.ShouldLineBeIncluded> lineIncludeFilters)
    {
        _lineIncludeFilters = lineIncludeFilters;
    }

    public async Task<uint> CountLines(Stream stream)
    {
        TextReader textReader = new StreamReader(stream);
        uint total = 0;
        string? line = "";

        do
        {
            line = await textReader.ReadLineAsync();
            if (line is null)
            {
                continue;
            }

            if (_lineIncludeFilters.All(includeFilter => includeFilter(line)))
            {
                total++;
            }

        } while (line is not null);
        
        return total;
    }
}