namespace LnWarn;

public static class LineFilterDelegates
{
    public delegate bool ShouldLineBeIncluded(string inputString);
}