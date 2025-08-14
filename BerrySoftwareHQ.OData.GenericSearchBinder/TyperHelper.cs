using System.Reflection;

namespace BerrySoftwareHQ.OData.GenericSearchBinder;

/// <summary>
/// Reflection/type utilities used by the search expression builders (nullability and property enumeration).
/// </summary>
internal static class TypeHelper
{
    /// <summary>
    /// Returns all public instance properties that are readable (filters out write-only/non-instance members)
    /// </summary>
    public static IEnumerable<PropertyInfo> GetReadableInstanceProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead);

    public static bool IsNullable(Type t) => Nullable.GetUnderlyingType(t) != null;
    public static Type Underlying(Type t) => Nullable.GetUnderlyingType(t) ?? t;
}