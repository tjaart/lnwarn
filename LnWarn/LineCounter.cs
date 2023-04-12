namespace LnWarn;

public class LineCounter
{
    public async Task<uint> CountLines(Stream stream)
    {
        TextReader textReader = new StreamReader(stream);
        uint total = 0;
        var line = await textReader.ReadLineAsync();
        while (line is not null)
        {
            line = await textReader.ReadLineAsync();
            total++;
        }

        ;
        return total;
    }
}