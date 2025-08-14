using System.Linq.Expressions;
using System.Reflection;
using BerrySoftwareHQ.OData.GenericSearchBinder.TypeSearchBuilders;

namespace BerrySoftwareHQ.OData.GenericSearchBinder;

/// <summary>
/// Builds boolean LINQ expressions that match a single search term against an entity's searchable properties.
/// Enumerates readable scalar properties and delegates per-type matching to specialized builders.
/// </summary>
internal static class PropertySearchExpressionBuilder
{
    /// <summary>
    /// Builds an "any property matches" expression for a single term across the entity's scalar properties
    /// </summary>
    public static Expression BuildPropertySearchExpression(string searchTerm, Type entityType,
        ParameterExpression parameter)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        var props = TypeHelper.GetReadableInstanceProperties(entityType);
        var expressions = new List<Expression>();

        foreach (var prop in props)
        {
            var expr = BuildTermExpressionForProperty(prop, parameter, searchTerm, lowerSearchTerm);
            if (expr != null)
            {
                expressions.Add(expr);
            }
        }

        if (expressions.Count == 0)
        {
            return Expression.Constant(false);
        }

        return CombineOr(expressions);
    }

    /// <summary>
    /// Reduces a list of expressions into e1 || e2 || ... (assumes at least one element)
    /// </summary>
    private static Expression CombineOr(IReadOnlyList<Expression> expressions)
    {
        if (expressions.Count == 1) return expressions[0];
        var result = expressions[0];
        for (var i = 1; i < expressions.Count; i++)
        {
            result = Expression.OrElse(result, expressions[i]);
        }

        return result;
    }

    private static bool IsNumeric(Type t)
    {
        t = TypeHelper.Underlying(t);
        return t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float) ||
               t == typeof(decimal);
    }

    private static Expression? BuildTermExpressionForProperty(PropertyInfo prop, ParameterExpression parameter,
        string originalSearch, string lowerSearch)
    {
        var propExpr = Expression.Property(parameter, prop);
        var type = prop.PropertyType;

        if (type == typeof(string))
        {
            return StringExpressionBuilder.BuildString(propExpr, lowerSearch);
        }

        if (type == typeof(bool) || type == typeof(bool?))
        {
            return BoolExpressionBuilder.BuildBool(propExpr, type, lowerSearch);
        }

        if (IsNumeric(type))
        {
            return StringExpressionBuilder.BuildToStringFallback(propExpr, type, lowerSearch);
        }

        if (type == typeof(DateTime) || type == typeof(DateTime?))
        {
            return DateTimeExpressionBuilder.BuildDateTime(propExpr, type, originalSearch, lowerSearch);
        }

        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
        {
            return DateOnlyExpressionBuilder.BuildDateOnly(propExpr, type, originalSearch, lowerSearch);
        }

        if (type == typeof(TimeOnly) || type == typeof(TimeOnly?))
        {
            return TimeOnlyExpressionBuilder.BuildTimeOnlyComposite(propExpr, type, originalSearch, lowerSearch);
        }

        if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
        {
            return DateTimeOffsetExpressionBuilder.BuildDateTimeOffset(propExpr, type, originalSearch, lowerSearch);
        }

        if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
        {
            return TimeSpanExpressionBuilder.BuildTimeSpan(propExpr, type, originalSearch, lowerSearch);
        }

        // Ignore other types (including navigations) by default
        return null;
    }
}