using System.Diagnostics.CodeAnalysis;

namespace LnWarn.Cmd;

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