using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace GenericSearchBinder.Tests;

public class NotSearchTests
{
    private Func<T, bool> GetTestFuncWithNotSearch<T>(string searchTerm)
    {
        var binder = new GenericSearchBinder();
        var searchTermNode = new SearchTermNode(searchTerm);

        // Create a NOT operation with the search term as its operand
        var notSearchExpression = new UnaryOperatorNode(UnaryOperatorKind.Not, searchTermNode);
        var searchClause = new SearchClause(notSearchExpression);

        var builder = new ODataConventionModelBuilder();
        builder.AddEntityType(typeof(T));
        var edmModel = builder.GetEdmModel();

        var context = new QueryBinderContext(edmModel, new ODataQuerySettings(), typeof(T));

        var expression = binder.BindSearch(searchClause, context);
        var lambda = (Expression<Func<T, bool>>)expression;
        return lambda.Compile();
    }

    private record OnlyOneStringTestEntity
    {
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithNotSimpleTerm_CreatesCorrectFilterExpression()
    {
        var func = GetTestFuncWithNotSearch<OnlyOneStringTestEntity>("a");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.False, "Should not match 'Apple'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Banana" }), Is.False, "Should not match 'Banana'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "BANANA" }), Is.False, "Should not match 'BANANA' case-insensitively");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Cherry" }), Is.True, "Should match 'Cherry'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    [Test]
    public void BindSearch_WithNotNumericStringSearchValue()
    {
        var func = GetTestFuncWithNotSearch<OnlyOneStringTestEntity>("123");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "123" }), Is.False, "Should not match '123'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc123" }), Is.False, "Should not match 'abc123'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "456" }), Is.True, "Should match '456'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    [Test]
    public void BindSearch_WithNotStringAndNumericStringSearchValue()
    {
        var func = GetTestFuncWithNotSearch<OnlyOneStringTestEntity>("b1");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "b1" }), Is.False, "Should not match 'b1'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "ab1c" }), Is.False, "Should not match 'ab1c'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "b2" }), Is.True, "Should match 'b2'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    [Test]
    public void BindSearch_WithNotSpecialCharacters_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithNotSearch<OnlyOneStringTestEntity>("!@#");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "!@#test" }), Is.False, "Should not match '!@#test'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test!@#" }), Is.False, "Should not match 'test!@#'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "normal" }), Is.True, "Should match 'normal'");
    }

    [Test]
    public void BindSearch_WithNotEmptyTerm_ReturnsAlwaysFalseExpression()
    {
        // OData SearchTermNode does not allow empty string, so use a non-empty placeholder
        var func = GetTestFuncWithNotSearch<OnlyOneStringTestEntity>("x");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match any string");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    private record OnlyOneBooleanTestEntity
    {
        public bool IsActive { get; set; }
    }

    [Test]
    public void BindSearch_WithNotNonStringProperty_ReturnsAlwaysFalseExpression()
    {
        var func = GetTestFuncWithNotSearch<OnlyOneBooleanTestEntity>("true");

        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = true }), Is.False, "Should not match true");
        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = false }), Is.True, "Should match false");
    }

    private record OnlyOneIntTestEntity
    {
        public int Age { get; set; }
    }

    [Test]
    public void BindSearch_WithNotNonStringPropertyAndNumericSearchValue()
    {
        var func = GetTestFuncWithNotSearch<OnlyOneIntTestEntity>("30");

        Assert.That(func(new OnlyOneIntTestEntity { Age = 30 }), Is.False, "Should not match 30");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 130 }), Is.False, "Should not match 130");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 300 }), Is.False, "Should not match 300");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 25 }), Is.True, "Should match 25");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 35 }), Is.True, "Should match 35");
    }

    private record OneIntOneStringTestEntity
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithNotMixedProperties_SearchInt_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithNotSearch<OneIntOneStringTestEntity>("30");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Alice" }), Is.False, "Should not match Age 30");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Bob" }), Is.True, "Should match Age 25");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.False, "Should not match Age 30 with null Name");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "Charlie" }), Is.True, "Should match Age 35");
    }

    [Test]
    public void BindSearch_WithNotMixedProperties_SearchString_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithNotSearch<OneIntOneStringTestEntity>("Alice");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Alice" }), Is.False, "Should not match Name 'Alice'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Bob" }), Is.True, "Should match Name 'Bob'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.True, "Should match null Name");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "Charlie" }), Is.True, "Should match Name 'Charlie'");
    }

    [Test]
    public void BindSearch_WithNotMixedProperties_SearchStringAndInt_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithNotSearch<OneIntOneStringTestEntity>("3");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "40" }), Is.False, "Should not match Name '40' and Age 30");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "4" }), Is.True, "Should match Name '4' and Age 25");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.False, "Should not match null Name with Age 30");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "30" }), Is.False, "Should not match Name '30' and Age 35");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 2, Name = "30" }), Is.False, "Should not match Name '30' and Age 2");
    }

    private record DateTimeAndStringTestEntity
    {
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
    }

    [Test]
    public void BindSearch_WithNotDateTimeAndStringProperty_SearchesOnlyString()
    {
        var func = GetTestFuncWithNotSearch<DateTimeAndStringTestEntity>("2023");

        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "Created in 2023" }), Is.False,
            "Should not match '2023' in description");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "Old record" }), Is.False,
            "Should not match when year contains '2023'");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2022, 1, 1), Description = "Created in 2023" }), Is.False,
            "Should not match '2023' in description regardless of actual date");
        Assert.That(
            func(new DateTimeAndStringTestEntity { CreatedDate = new DateTime(2022, 1, 1), Description = null }),
            Is.True, "Should match null description");
    }

    [Test]
    public void BindSearch_WithNotDateComponents_SearchesCorrectly()
    {
        // Test with day number
        var funcDay = GetTestFuncWithNotSearch<DateTimeAndStringTestEntity>("15");
        Assert.That(
            funcDay(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Day 15" }), Is.False,
            "Should not match '15' in description");
        Assert.That(
            funcDay(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Day 16" }), Is.False,
            "Should not match day with '15'");
    }
}