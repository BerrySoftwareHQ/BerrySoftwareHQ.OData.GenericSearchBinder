using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.Tests;

public class OrSearchTests
{
    private Func<T, bool> GetTestFuncWithOrSearch<T>(string firstSearchTerm, string secondSearchTerm)
    {
        var binder = new GenericSearchBinder();
        if (string.IsNullOrEmpty(firstSearchTerm) || string.IsNullOrEmpty(secondSearchTerm))
        {
            return _ => true;
        }
        var firstSearchTermNode = new SearchTermNode(firstSearchTerm);
        var secondSearchTermNode = new SearchTermNode(secondSearchTerm);
        var orSearchExpression = new BinaryOperatorNode(BinaryOperatorKind.Or, firstSearchTermNode, secondSearchTermNode);
        var searchClause = new SearchClause(orSearchExpression);
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
    public void BindSearch_WithOrSimpleTerms_CreatesCorrectFilterExpression()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("a", "c");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match 'Apple' (contains 'a')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Banana" }), Is.True, "Should match 'Banana' (contains 'a')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Cherry" }), Is.True, "Should match 'Cherry' (contains 'c')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Dog" }), Is.False, "Should not match 'Dog' (missing both 'a' and 'c')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithOrNumericStringSearchValue()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("1", "2");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "123" }), Is.True, "Should match '123' (contains both '1' and '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc1xyz" }), Is.True, "Should match 'abc1xyz' (contains '1')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc2xyz" }), Is.True, "Should match 'abc2xyz' (contains '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc3xyz" }), Is.False, "Should not match 'abc3xyz' (missing both '1' and '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithOrStringAndNumericStringSearchValue()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("a", "1");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "a1" }), Is.True, "Should match 'a1'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc" }), Is.True, "Should match 'abc' (contains 'a')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "123" }), Is.True, "Should match '123' (contains '1')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "xyz" }), Is.False, "Should not match 'xyz' (missing both 'a' and '1')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithOrSpecialCharacters_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("!@", "#$");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "!@test" }), Is.True, "Should match '!@test'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test#$" }), Is.True, "Should match 'test#$'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "!@#$test" }), Is.True, "Should match '!@#$test'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "normal" }), Is.False, "Should not match 'normal'");
    }

    [Test]
    public void BindSearch_WithOrEmptyTerms_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("", "");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match any string");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    [Test]
    public void BindSearch_WithOrOneEmptyTerm_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("a", "");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match 'Apple'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Banana" }), Is.True, "Should match 'Banana'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Cherry" }), Is.True, "Should match 'Cherry' due to empty term");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property due to empty term");
    }

    private record OnlyOneBooleanTestEntity
    {
        public bool IsActive { get; set; }
    }

    [Test]
    public void BindSearch_WithOrNonStringProperty_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneBooleanTestEntity>("true", "active");

        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = true }), Is.True, "Should match true");
        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = false }), Is.False, "Should match false");
    }

    private record OnlyOneIntTestEntity
    {
        public int Age { get; set; }
    }

    [Test]
    public void BindSearch_WithOrNonStringPropertyAndNumericSearchValue()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneIntTestEntity>("3", "5");

        Assert.That(func(new OnlyOneIntTestEntity { Age = 30 }), Is.True, "Should match 30 (contains '3')");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 50 }), Is.True, "Should match 50 (contains '5')");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 35 }), Is.True, "Should match 35 (contains both '3' and '5')");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 21 }), Is.False, "Should not match 21 (missing both '3' and '5')");
    }

    private record MultipleStringPropertiesTestEntity
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    [Test]
    public void BindSearch_WithOrTermsInDifferentProperties_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithOrSearch<MultipleStringPropertiesTestEntity>("john", "jane");

        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "John", LastName = "Doe" }), Is.True, "Should match 'John Doe'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "Jane", LastName = "Smith" }), Is.True, "Should match 'Jane Smith'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "James", LastName = "Johnson" }), Is.True, "Should match 'Johnson'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = null, LastName = "John" }), Is.True, "Should match null FirstName but LastName contains 'john'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = null, LastName = null }), Is.False, "Should not match null properties");
    }

    private record OneIntOneStringTestEntity
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithOrMixedProperties_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithOrSearch<OneIntOneStringTestEntity>("3", "alice");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Bob" }), Is.True, "Should match Age 30 and Name 'Bob'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Alice" }), Is.True, "Should match Age 25 and Name 'Alice'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Alice" }), Is.True, "Should match Age 30 and Name 'Alice'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Bob" }), Is.False, "Should not match Age 25 and Name 'Bob'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.True, "Should match Age 30 with null Name");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = null }), Is.False, "Should not match Age 25 with null Name");
    }

    [Test]
    public void BindSearch_WithOrSameTerms_ReturnsEquivalentToSingleTerm()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneStringTestEntity>("test", "test");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "test" }), Is.True, "Should match 'test'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Test123" }), Is.True, "Should match 'Test123'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc" }), Is.False, "Should not match 'abc'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    private record OnlyOneDateTimeTestEntity
    {
        public DateTime CreatedDate { get; set; }
    }

    [Test]
    public void BindSearch_WithOrDateTimeProperty_SearchesCorrectly()
    {
        var func = GetTestFuncWithOrSearch<OnlyOneDateTimeTestEntity>("2023", "01");

        // Either the year 2023 or month 01 should match
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2023, 1, 15) }), Is.True, 
            "Should match 2023-01-15");
        // Year 2023 but not month 01
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2023, 2, 15) }), Is.True, 
            "Should match 2023-02-15 (contains '2023')");
        // Month 01 but not year 2023
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2022, 1, 15) }), Is.True, 
            "Should match 2022-01-15 (contains '01')");
        // Neither year 2023 nor month 01
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2022, 2, 15) }), Is.False, 
            "Should not match 2022-02-15 (missing both '2023' and '01')");
    }

    private record DateTimeAndStringTestEntity
    {
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
    }

    [Test]
    public void BindSearch_WithOrDateTimeAndStringProperty_SearchesOnlyString()
    {
        var func = GetTestFuncWithOrSearch<DateTimeAndStringTestEntity>("2023", "event");

        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "2023 event" }), Is.True,
            "Should match '2023' and 'event' in description");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "2023 meeting" }), Is.True,
            "Should match when description contains '2023'");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2022, 1, 1), Description = "event in 2022" }), Is.True,
            "Should match when description contains 'event'");
        Assert.That(
            func(new DateTimeAndStringTestEntity { CreatedDate = new DateTime(2022, 1, 1), Description = "meeting" }),
            Is.False, "Should not match when description contains neither '2023' nor 'event'");
        Assert.That(
            func(new DateTimeAndStringTestEntity { CreatedDate = new DateTime(2023, 1, 1), Description = null }),
            Is.True, "Should match null description");
    }

    [Test]
    public void BindSearch_WithOrDateComponents_SearchesCorrectly()
    {
        // Test with day and month
        var funcDayMonth = GetTestFuncWithOrSearch<DateTimeAndStringTestEntity>("15", "01");
        Assert.That(
            funcDayMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event on day 15 of month 01" }), Is.True,
            "Should match when description contains both '15' and '01'");
        Assert.That(
            funcDayMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event on the 15th" }), Is.True,
            "Should match when description contains '15'");
        Assert.That(
            funcDayMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "January event" }), Is.True,
            "Should match when description contains '01'");
        Assert.That(
            funcDayMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 2, 20), Description = "February event" }), Is.False,
            "Should not match when description contains neither '15' nor '01'");
    }

    private record EntityWithNullableDateTime
    {
        public DateTime? OptionalDate { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithOrNullableDateTimeProperty_HandlesNullCorrectly()
    {
        var func = GetTestFuncWithOrSearch<EntityWithNullableDateTime>("2023", "event");

        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = new DateTime(2023, 1, 1), Name = "2023 event" }),
            Is.True, "Should match '2023' and 'event' in name");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = "2023 event" }), Is.True,
            "Should match '2023' and 'event' in name when date is null");
        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = null, Name = "2023 meeting" }),
            Is.True, "Should match when name contains '2023'");
        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = null, Name = "Important event" }),
            Is.True, "Should match when name contains 'event'");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = "meeting" }), Is.False,
            "Should not match when name contains neither '2023' nor 'event'");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = null }), Is.False,
            "Should not match when both properties are null");
    }
}