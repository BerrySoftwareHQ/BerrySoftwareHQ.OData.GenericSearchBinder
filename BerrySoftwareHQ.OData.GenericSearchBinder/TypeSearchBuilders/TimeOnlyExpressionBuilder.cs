using System.Globalization;
using System.Linq.Expressions;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

/// <summary>
/// Builds TimeOnly search expressions (substring match plus hour and exact time helpers).
/// </summary>
internal static class TimeOnlyExpressionBuilder
{
    /// <summary>
    /// TimeOnly matching: substring on ToString plus fallbacks for Hour equality and exact time formats
    /// </summary>
    public static Expression BuildTimeOnlyComposite(Expression propertyExpr, Type propertyType, string originalSearch,
        string lowerSearch)
    {
        // Base substring contains on ToString()
        var containsPart = StringExpressionBuilder.BuildToStringFallback(propertyExpr, propertyType, lowerSearch);

        Expression? hourEq = null;
        if (int.TryParse(originalSearch, out var hour) && hour >= 0 && hour <= 23)
        {
            if (propertyType == typeof(TimeOnly))
            {
                var hr = Expression.Property(propertyExpr, nameof(TimeOnly.Hour));
                hourEq = Expression.Equal(hr, Expression.Constant(hour));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var hr = Expression.Property(val, nameof(TimeOnly.Hour));
                var eq = Expression.Equal(hr, Expression.Constant(hour));
                hourEq = Expression.AndAlso(hasValue, eq);
            }
        }

        Expression? exactTimeEq = null;
        string[] exactFormats = ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];
        if (TimeOnly.TryParseExact(originalSearch, exactFormats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var exactTime))
        {
            if (propertyType == typeof(TimeOnly))
            {
                exactTimeEq = Expression.Equal(propertyExpr, Expression.Constant(exactTime));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var eq = Expression.Equal(val, Expression.Constant(exactTime));
                exactTimeEq = Expression.AndAlso(hasValue, eq);
            }
        }

        var combined = containsPart;
        if (hourEq != null) combined = Expression.OrElse(combined, hourEq);
        if (exactTimeEq != null) combined = Expression.OrElse(combined, exactTimeEq);
        return combined;
    }
}