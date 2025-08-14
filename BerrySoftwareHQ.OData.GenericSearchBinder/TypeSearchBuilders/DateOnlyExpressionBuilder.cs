using System.Globalization;
using System.Linq.Expressions;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

/// <summary>
/// Builds DateOnly search expressions (substring match plus year and exact date helpers).
/// </summary>
internal static class DateOnlyExpressionBuilder
{
    /// <summary>
    /// DateOnly matching: substring on ToString plus fallbacks for Year and exact yyyy-MM-dd
    /// </summary>
    public static Expression BuildDateOnly(Expression propertyExpr, Type propertyType, string originalSearch,
        string lowerSearch)
    {
        // Base substring contains on ToString()
        var containsPart = StringExpressionBuilder.BuildToStringFallback(propertyExpr, propertyType, lowerSearch);

        Expression? yearEq = null;
        if (int.TryParse(originalSearch, out var year) && year >= 1 && year <= 9999)
        {
            if (propertyType == typeof(DateOnly))
            {
                var yr = Expression.Property(propertyExpr, nameof(DateOnly.Year));
                yearEq = Expression.Equal(yr, Expression.Constant(year));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var yr = Expression.Property(val, nameof(DateOnly.Year));
                var eq = Expression.Equal(yr, Expression.Constant(year));
                yearEq = Expression.AndAlso(hasValue, eq);
            }
        }

        Expression? exactDateEq = null;
        if (DateOnly.TryParseExact(originalSearch, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var exactDateOnly))
        {
            if (propertyType == typeof(DateOnly))
            {
                exactDateEq = Expression.Equal(propertyExpr, Expression.Constant(exactDateOnly));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var eq = Expression.Equal(val, Expression.Constant(exactDateOnly));
                exactDateEq = Expression.AndAlso(hasValue, eq);
            }
        }

        var combined = containsPart;
        if (yearEq != null) combined = Expression.OrElse(combined, yearEq);
        if (exactDateEq != null) combined = Expression.OrElse(combined, exactDateEq);
        return combined;
    }
}