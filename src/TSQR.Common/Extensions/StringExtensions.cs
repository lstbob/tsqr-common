using TSQR.Common.Errors;
using TSQR.Common.Results;

namespace TSQR.Common.Extensions;

public static class StringExtensions
{
    public static Result<string> Validate(this string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ValidationError(paramName, $"{paramName} cannot be null or whitespace.");

        return value!;
    }
}
