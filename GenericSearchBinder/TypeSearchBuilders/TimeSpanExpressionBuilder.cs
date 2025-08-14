using System.Globalization;
using System.Linq.Expressions;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

/// <summary>
/// Builds TimeSpan search expressions (substring match plus hour and exact time helpers).
/// </summary>
internal static class TimeSpanExpressionBuilder
{
    /// <summary>
    /// TimeSpan matching: substring on ToString plus fallbacks for Hours equality and exact time formats
    /// </summary>
    public static Expression BuildTimeSpan(Expression propertyExpr, Type propertyType, string originalSearch,
        string lowerSearch)
    {
        // Base substring contains on ToString()
        var containsPart = StringExpressionBuilder.BuildToStringFallback(propertyExpr, propertyType, lowerSearch);

        Expression? hourEq = null;
        if (int.TryParse(originalSearch, out var hour) && hour >= 0 && hour <= 23)
        {
            if (propertyType == typeof(TimeSpan))
            {
                var hr = Expression.Property(propertyExpr, nameof(TimeSpan.Hours));
                hourEq = Expression.Equal(hr, Expression.Constant(hour));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var hr = Expression.Property(val, nameof(TimeSpan.Hours));
                var eq = Expression.Equal(hr, Expression.Constant(hour));
                hourEq = Expression.AndAlso(hasValue, eq);
            }
        }

        Expression? exactTimeEq = null;
        string[] exactFormats =
            ["hh\\:mm", "hh\\:mm\\:ss", "hh\\:mm\\:ss\\.FFFFFFF"]; // TimeSpan formats require escaped colon
        if (TimeSpan.TryParseExact(originalSearch, exactFormats, CultureInfo.InvariantCulture, out var exactSpan))
        {
            if (propertyType == typeof(TimeSpan))
            {
                exactTimeEq = Expression.Equal(propertyExpr, Expression.Constant(exactSpan));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var eq = Expression.Equal(val, Expression.Constant(exactSpan));
                exactTimeEq = Expression.AndAlso(hasValue, eq);
            }
        }

        var combined = containsPart;
        if (hourEq != null) combined = Expression.OrElse(combined, hourEq);
        if (exactTimeEq != null) combined = Expression.OrElse(combined, exactTimeEq);
        return combined;
    }
}