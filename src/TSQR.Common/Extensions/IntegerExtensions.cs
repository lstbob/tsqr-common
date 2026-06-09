using TSQR.Common.Errors;
using TSQR.Common.Results;

namespace TSQR.Common.Extensions;

public static class IntegerExtensions
{
    public static Result<int> ValidatePositive(this int value, string paramName)
    {
        if (value <= 0)
            return new ValidationError(paramName, $"{paramName} must be positive.");

        return value;
    }
}
