using System.Linq.Expressions;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

/// <summary>
/// Builds boolean equality expressions when the search term is 'true' or 'false' (handles nullable bools).
/// </summary>
internal static class BoolExpressionBuilder
{
    /// <summary>
    /// If the term is exactly "true" or "false", build an equality comparison (handles nullable bool)
    /// </summary>
    public static Expression? BuildBool(Expression boolProperty, Type propertyType,
        string lowerSearch)
    {
        if (!string.Equals(lowerSearch, bool.TrueString, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(lowerSearch, bool.FalseString, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var boolValue = string.Equals(lowerSearch, bool.TrueString, StringComparison.OrdinalIgnoreCase);
        // For nullable bool, constant type should match
        var constant = Expression.Constant(boolValue, propertyType);
        return Expression.Equal(boolProperty, constant);
    }
}