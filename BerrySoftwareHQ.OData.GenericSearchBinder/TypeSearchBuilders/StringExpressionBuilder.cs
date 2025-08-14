using System.Linq.Expressions;
using System.Reflection;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

/// <summary>
/// Builds case-insensitive string Contains expressions and ToString-based fallbacks for non-strings.
/// </summary>
internal static class StringExpressionBuilder
{
    private static readonly MethodInfo StringToLower =
        typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [
        typeof(string)
    ])!;

    /// <summary>
    /// Case-insensitive Contains for string properties with a null-check guard
    /// </summary>
    public static Expression BuildString(Expression stringProperty, string lowerSearch)
    {
        var notNull = Expression.NotEqual(stringProperty, Expression.Constant(null, typeof(string)));
        var toLower = Expression.Call(stringProperty, StringToLower);
        var contains = Expression.Call(toLower, StringContains, Expression.Constant(lowerSearch));
        return Expression.AndAlso(notNull, contains);
    }

    /// <summary>
    /// Converts the property to string (server-translatable) and applies case-insensitive substring Contains
    /// </summary>
    public static Expression BuildToStringFallback(Expression propertyExpr, Type propertyType, string lowerSearch)
    {
        // Numeric and DateTime substring search via parameterless ToString().ToLower().Contains with nullable handling
        var underlying = TypeHelper.Underlying(propertyType);
        var toString = underlying.GetMethod(nameof(ToString), Type.EmptyTypes)!;

        if (TypeHelper.IsNullable(propertyType))
        {
            var hasValue = Expression.Property(propertyExpr, "HasValue");
            var val = Expression.Property(propertyExpr, "Value");
            var str = Expression.Call(val, toString);
            var lower = Expression.Call(str, StringToLower);
            var contains = Expression.Call(lower, StringContains, Expression.Constant(lowerSearch));
            return Expression.AndAlso(hasValue, contains);
        }
        else
        {
            var str = Expression.Call(propertyExpr, toString);
            var lower = Expression.Call(str, StringToLower);
            return Expression.Call(lower, StringContains, Expression.Constant(lowerSearch));
        }
    }
}