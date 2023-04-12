using FluentAssertions;

namespace LnWarn.Tests;

public class LineCounterTests
{
    private readonly LineCounter _subject;

    public LineCounterTests()
    {
        _subject = new LineCounter();
    }
    
    [Theory]
    [InlineData(3, 3)]
    [InlineData(16, 16)]
    public async Task CountLines_ShouldReturnLineCount(int lineCount, uint expectedLineCount)
    {
        // arrange
        var text = Enumerable.Range(1, lineCount).Select(c=>$"line {c}").Aggregate((s, s1) => $"{s}\n{s1}");
        var stream = GenerateStreamFromString(text);

        // act
        var result = await _subject.CountLines(stream);
        
        // assert
        result.Should().Be(expectedLineCount);
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