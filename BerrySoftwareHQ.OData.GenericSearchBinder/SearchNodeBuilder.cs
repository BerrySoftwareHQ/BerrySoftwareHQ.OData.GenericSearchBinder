using System.Linq.Expressions;
using Microsoft.OData.UriParser;

namespace BerrySoftwareHQ.OData.GenericSearchBinder;

/// <summary>
/// Binds OData $search AST nodes to a boolean LINQ Expression for a given entity type.
/// Translates terms and logical operators (AND/OR/NOT) into an expression tree.
/// </summary>
internal static class SearchNodeBinder
{
    /// <summary>
    /// Dispatches on the OData search AST node type and builds the corresponding boolean expression
    /// </summary>
    public static Expression BindNode(QueryNode node, Type entityType, ParameterExpression parameter)
    {
        return node switch
        {
            SearchTermNode term => BindTerm(term, entityType, parameter),
            BinaryOperatorNode bin => BuildBinary(bin, entityType, parameter),
            UnaryOperatorNode unary => BuildUnary(unary, entityType, parameter),
            _ => throw new NotSupportedException($"Unsupported search node: {node.GetType().Name}")
        };
    }

    private static bool IsEmptySearchTerm(QueryNode n)
        => n is SearchTermNode t && string.IsNullOrEmpty(t.Text);

    private static string GetSearchText(QueryNode n)
        => n is SearchTermNode t ? t.Text ?? string.Empty : string.Empty;

    /// <summary>
    /// Builds expression for a single search term (matches any applicable property)
    /// </summary>
    private static Expression BindTerm(SearchTermNode termNode, Type entityType, ParameterExpression parameter)
    {
        if (string.IsNullOrEmpty(termNode.Text))
        {
            // OData parser shouldn't yield empty SearchTermNode, but guard for safety
            return Expression.Constant(true);
        }

        return PropertySearchExpressionBuilder.BuildPropertySearchExpression(termNode.Text, entityType, parameter);
    }

    /// <summary>
    /// Builds expression for AND/OR nodes
    /// </summary>
    private static Expression BuildBinary(BinaryOperatorNode binNode, Type entityType, ParameterExpression parameter)
    {
        var leftEmpty = IsEmptySearchTerm(binNode.Left);
        var rightEmpty = IsEmptySearchTerm(binNode.Right);

        if (binNode.OperatorKind == BinaryOperatorKind.And)
        {
            if (leftEmpty && rightEmpty) return Expression.Constant(true);
            if (leftEmpty) return BindNode(binNode.Right, entityType, parameter);
            if (rightEmpty) return BindNode(binNode.Left, entityType, parameter);

            var left = PropertySearchExpressionBuilder.BuildPropertySearchExpression(GetSearchText(binNode.Left),
                entityType, parameter);
            var right = PropertySearchExpressionBuilder.BuildPropertySearchExpression(GetSearchText(binNode.Right),
                entityType, parameter);
            return Expression.AndAlso(left, right);
        }

        if (binNode.OperatorKind == BinaryOperatorKind.Or)
        {
            if (leftEmpty || rightEmpty) return Expression.Constant(true);

            var left = PropertySearchExpressionBuilder.BuildPropertySearchExpression(GetSearchText(binNode.Left),
                entityType, parameter);
            var right = PropertySearchExpressionBuilder.BuildPropertySearchExpression(GetSearchText(binNode.Right),
                entityType, parameter);
            return Expression.OrElse(left, right);
        }

        throw new NotSupportedException($"Unsupported binary operator: {binNode.OperatorKind}");
    }

    /// <summary>
    /// Builds expression for NOT nodes
    /// </summary>
    private static Expression BuildUnary(UnaryOperatorNode unaryNode, Type entityType, ParameterExpression parameter)
    {
        if (IsEmptySearchTerm(unaryNode.Operand))
        {
            // NOT of empty term is always false
            return Expression.Constant(false);
        }

        if (unaryNode.Operand is SearchTermNode t)
        {
            var inner = PropertySearchExpressionBuilder.BuildPropertySearchExpression(t.Text, entityType, parameter);
            return Expression.Not(inner);
        }

        var operand = BindNode(unaryNode.Operand, entityType, parameter);
        return Expression.Not(operand);
    }
}