using System.Globalization;
using System.Linq.Expressions;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

/// <summary>
/// Builds DateTimeOffset search expressions (substring match plus year and exact date helpers).
/// </summary>
internal static class DateTimeOffsetExpressionBuilder
{
    /// <summary>
    /// DateTimeOffset matching: substring on ToString plus EF-friendly fallbacks for Year and exact yyyy-MM-dd (Date part)
    /// </summary>
    public static Expression BuildDateTimeOffset(Expression propertyExpr, Type propertyType, string originalSearch,
        string lowerSearch)
    {
        // Base substring contains on ToString()
        var containsPart = StringExpressionBuilder.BuildToStringFallback(propertyExpr, propertyType, lowerSearch);

        Expression? yearEq = null;
        if (int.TryParse(originalSearch, out var year) && year >= 1 && year <= 9999)
        {
            if (propertyType == typeof(DateTimeOffset))
            {
                var yr = Expression.Property(propertyExpr, nameof(DateTimeOffset.Year));
                yearEq = Expression.Equal(yr, Expression.Constant(year));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var yr = Expression.Property(val, nameof(DateTimeOffset.Year));
                var eq = Expression.Equal(yr, Expression.Constant(year));
                yearEq = Expression.AndAlso(hasValue, eq);
            }
        }

        Expression? exactDateEq = null;
        if (DateTime.TryParseExact(originalSearch, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var exactDate))
        {
            if (propertyType == typeof(DateTimeOffset))
            {
                var dateProp = Expression.Property(propertyExpr, nameof(DateTimeOffset.Date));
                exactDateEq = Expression.Equal(dateProp, Expression.Constant(exactDate.Date));
            }
            else
            {
                var hasValue = Expression.Property(propertyExpr, "HasValue");
                var val = Expression.Property(propertyExpr, "Value");
                var dateProp = Expression.Property(val, nameof(DateTimeOffset.Date));
                var eq = Expression.Equal(dateProp, Expression.Constant(exactDate.Date));
                exactDateEq = Expression.AndAlso(hasValue, eq);
            }
        }

        var combined = containsPart;
        if (yearEq != null) combined = Expression.OrElse(combined, yearEq);
        if (exactDateEq != null) combined = Expression.OrElse(combined, exactDateEq);
        return combined;
    }
}