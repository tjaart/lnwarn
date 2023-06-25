namespace LnWarn.Tests;

public class LineCounterTests
{
    private readonly LineCounter _subject;
    private Func<string, bool> _redirectLineFunc = _ => true;

    public LineCounterTests()
    {
        bool MockLineFilter(string inputString) => _redirectLineFunc(inputString);

        _subject = new LineCounter(new List<LineFilterDelegates.ShouldLineBeIncluded> { MockLineFilter });
    }
    
    [Theory]
    [InlineData(3, 3)]
    [InlineData(16, 16)]
    public async Task CountLines_ShouldReturnLineCount(int lineCount, uint expectedLineCount)
    {
        // arrange
        var stream = CreateStreamOfLines(lineCount);

        // act
        var result = await _subject.CountLines(stream);
        
        // assert
        result.Should().Be(expectedLineCount);
    }

    [Fact]
    public async Task CountLines_ShouldCountLines_WhenFiltersIncludeLines()
    {
        // arrange
        var stream = GenerateStreamFromString("""
        12345
        123
        1234
        123456
        """);
        _redirectLineFunc = s => s.Length >= 5;
        
        // act
        var result = await _subject.CountLines(stream);
        
        // assert
        result.Should().Be(2);
    }

    private static Stream CreateStreamOfLines(int lineCount)
    {
        var text = Enumerable.Range(1, lineCount).Select(c => $"line {c}").Aggregate((s, s1) => $"{s}\n{s1}");
        var stream = GenerateStreamFromString(text);
        return stream;
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}

