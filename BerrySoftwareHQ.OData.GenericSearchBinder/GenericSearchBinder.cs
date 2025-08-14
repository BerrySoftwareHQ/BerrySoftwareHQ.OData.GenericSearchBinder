using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.UriParser;

namespace BerrySoftwareHQ.OData.GenericSearchBinder;

/// <summary>
/// Provides a simplified and robust implementation of ISearchBinder for OData search operations.
/// This binder performs a case-insensitive "contains" search on all readable string properties of an entity.
/// It is designed to be safe and efficient, ignoring non-string properties to prevent errors.
/// </summary>
public class GenericSearchBinder : ISearchBinder
{
    /// <summary>
    /// Entry point: builds a lambda (e => bool) that represents the $search predicate for the entity type
    /// </summary>
    public Expression BindSearch(SearchClause searchClause, QueryBinderContext context)
    {
        var parameter = Expression.Parameter(context.ElementClrType, "e");
        var expr = SearchNodeBinder.BindNode(searchClause.Expression, context.ElementClrType, parameter);
        return Expression.Lambda(expr, parameter);
    }
}