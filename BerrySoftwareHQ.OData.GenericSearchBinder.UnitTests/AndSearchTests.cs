using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.Tests;

public class AndSearchTests
{
    private static Func<T, bool> GetTestFuncWithAndSearch<T>(string firstSearchTerm, string secondSearchTerm)
    {
        if (string.IsNullOrEmpty(firstSearchTerm) || string.IsNullOrEmpty(secondSearchTerm))
        {
            return _ => true;
        }
        var binder = new GenericSearchBinder();
        var firstSearchTermNode = new SearchTermNode(firstSearchTerm);
        var secondSearchTermNode = new SearchTermNode(secondSearchTerm);
        var andSearchExpression = new BinaryOperatorNode(BinaryOperatorKind.And, firstSearchTermNode, secondSearchTermNode);
        var searchClause = new SearchClause(andSearchExpression);
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
    public void BindSearch_WithAndSimpleTerms_CreatesCorrectFilterExpression()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("a", "b");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.False, "Should not match 'Apple' (missing 'b')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Banana" }), Is.True, "Should match 'Banana' (contains both 'a' and 'b')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "BANANA" }), Is.True, "Should match 'BANANA' case-insensitively");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Cherry" }), Is.False, "Should not match 'Cherry' (missing 'a')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Ball" }), Is.True, "Should match 'Ball' ");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithAndNumericStringSearchValue()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("1", "2");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "123" }), Is.True, "Should match '123' (contains both '1' and '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc12xyz" }), Is.True, "Should match 'abc12xyz' (contains both '1' and '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc1xyz" }), Is.False, "Should not match 'abc1xyz' (missing '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc2xyz" }), Is.False, "Should not match 'abc2xyz' (missing '1')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc3xyz" }), Is.False, "Should not match 'abc3xyz' (missing both '1' and '2')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithAndStringAndNumericStringSearchValue()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("a", "1");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "a1" }), Is.True, "Should match 'a1'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc123" }), Is.True, "Should match 'abc123'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc" }), Is.False, "Should not match 'abc' (missing '1')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "123" }), Is.False, "Should not match '123' (missing 'a')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithAndSpecialCharacters_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("!@", "#$");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "!@#$test" }), Is.True, "Should match '!@#$test'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test!@#$" }), Is.True, "Should match 'test!@#$'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test!@" }), Is.False, "Should not match 'test!@' (missing '#$')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test#$" }), Is.False, "Should not match 'test#$' (missing '!@')");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "normal" }), Is.False, "Should not match 'normal'");
    }

    [Test]
    public void BindSearch_WithAndEmptyTerms_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("", "");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match any string");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    [Test]
    public void BindSearch_WithAndOneEmptyTerm_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("a", "");

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
    public void BindSearch_WithAndNonStringProperty_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneBooleanTestEntity>("true", "active");

        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = true }), Is.False, "Should match false");
        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = false }), Is.False, "Should match false");
    }

    private record OnlyOneIntTestEntity
    {
        public int Age { get; set; }
    }

    [Test]
    public void BindSearch_WithAndNonStringPropertyAndNumericSearchValue()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneIntTestEntity>("3", "0");

        Assert.That(func(new OnlyOneIntTestEntity { Age = 30 }), Is.True, "Should match 30");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 130 }), Is.True, "Should match 130");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 300 }), Is.True, "Should match 300");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 25 }), Is.False, "Should not match 25");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 35 }), Is.False, "Should not match 35");
    }

    private record MultipleStringPropertiesTestEntity
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    [Test]
    public void BindSearch_WithAndTermsInDifferentProperties_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithAndSearch<MultipleStringPropertiesTestEntity>("john", "doe");

        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "John", LastName = "Doe" }), Is.True, "Should match 'John Doe'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "John", LastName = "Smith" }), Is.False, "Should not match 'John Smith'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "Jane", LastName = "Doe" }), Is.False, "Should not match 'Jane Doe'");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = "JohnDoe", LastName = null }), Is.True, "Should match 'JohnDoe' in first name");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = null, LastName = "JohnDoe" }), Is.True, "Should match 'JohnDoe' in last name");
        Assert.That(func(new MultipleStringPropertiesTestEntity { FirstName = null, LastName = null }), Is.False, "Should not match null properties");
    }

    private record OneIntOneStringTestEntity
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithAndMixedProperties_ReturnsCorrectExpression()
    {
        var func = GetTestFuncWithAndSearch<OneIntOneStringTestEntity>("3", "a");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Alice" }), Is.True, "Should match Age 30 and Name 'Alice'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Alice" }), Is.False, "Should not match Age 25 and Name 'Alice'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Bob" }), Is.False, "Should not match Age 30 and Name 'Bob'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.False, "Should not match Age 30 with null Name");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "Charlie" }), Is.True, "Should match match Age 35 and Name 'Charlie'");
    }

    [Test]
    public void BindSearch_WithAndSameTerms_ReturnsEquivalentToSingleTerm()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneStringTestEntity>("test", "test");

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
    public void BindSearch_WithAndDateTimeProperty_SearchesCorrectly()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneDateTimeTestEntity>("2023", "2023-01-15");

        // Both the year 2023 and month 01 should match
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2023, 1, 15) }), Is.True, 
            "Should match 2023-01-15");
        // Year 2023 but not month 01
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2023, 2, 15) }), Is.False, 
            "Should not match 2023-02-15 (missing '01')");
        // Month 01 but not year 2023
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2022, 1, 15) }), Is.False, 
            "Should not match 2022-01-15 (missing '2023')");
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
    public void BindSearch_WithAndDateTimeAndStringProperty_SearchesOnlyString()
    {
        var func = GetTestFuncWithAndSearch<DateTimeAndStringTestEntity>("2023", "event");

        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "2023 event" }), Is.True,
            "Should match '2023' and 'event' in description");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "2023 meeting" }), Is.False,
            "Should not match when description contains '2023' but not 'event'");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2022, 1, 1), Description = "event in 2022" }), Is.False,
            "Should not match when description contains 'event' but not '2023'");
        Assert.That(
            func(new DateTimeAndStringTestEntity { CreatedDate = new DateTime(2023, 1, 1), Description = null }),
            Is.False, "Should not match null description");
    }

    [Test]
    public void BindSearch_WithAndDateComponents_SearchesCorrectly()
    {
        // Test with day and month
        var funcDayMonth = GetTestFuncWithAndSearch<DateTimeAndStringTestEntity>("15", "01");
        Assert.That(
            funcDayMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event on day 15 of month 01" }), Is.True,
            "Should match when description contains both '15' and '01'");
        Assert.That(
            funcDayMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event on the 15th" }), Is.True,
            "Should match when description contains both '01' and '15'");

        // Test with month and year
        var funcMonthYear = GetTestFuncWithAndSearch<DateTimeAndStringTestEntity>("01", "2023");
        Assert.That(
            funcMonthYear(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "January 2023 event" }), Is.True,
            "Should match when description contains both '01' and '2023'");
        Assert.That(
            funcMonthYear(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 2, 15), Description = "February 2023 event" }), Is.False,
            "Should not match when description contains '2023' but not '01'");
    }

    private record EntityWithNullableDateTime
    {
        public DateTime? OptionalDate { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithAndNullableDateTimeProperty_HandlesNullCorrectly()
    {
        var func = GetTestFuncWithAndSearch<EntityWithNullableDateTime>("2023", "event");

        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = new DateTime(2023, 1, 1), Name = "2023 event" }),
            Is.True, "Should match '2023' and 'event' in name");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = "2023 event" }), Is.True,
            "Should match '2023' and 'event' in name when date is null");
        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = null, Name = "2023 meeting" }),
            Is.False, "Should not match when name contains '2023' but not 'event'");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = null }), Is.False,
            "Should not match when both properties are null");
    }


    private record OnlyOneTimeSpanEntity
    {
        public TimeSpan Duration { get; set; }
    }

    private record OnlyNullableTimeSpanEntity
    {
        public TimeSpan? Duration { get; set; }
    }

    [Test]
    public void BindSearch_WithAnd_TimeSpan_SubstringAndHourFallback()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneTimeSpanEntity>("01:00", "1");

        Assert.That(func(new OnlyOneTimeSpanEntity { Duration = new TimeSpan(1, 0, 0) }), Is.True,
            "Should match: contains '01:00' and hour=1 satisfied by same property" );
        Assert.That(func(new OnlyOneTimeSpanEntity { Duration = new TimeSpan(2, 0, 0) }), Is.False,
            "Should not match: hour=2 does not satisfy '1'");
        Assert.That(func(new OnlyOneTimeSpanEntity { Duration = new TimeSpan(0, 59, 0) }), Is.False,
            "Should not match: lacks '01:00' and hour 1");
    }

    [Test]
    public void BindSearch_WithAnd_NullableTimeSpan_WithExactFormat()
    {
        var func = GetTestFuncWithAndSearch<OnlyNullableTimeSpanEntity>("01:00:00", "1");

        Assert.That(func(new OnlyNullableTimeSpanEntity { Duration = new TimeSpan(1, 0, 0) }), Is.True,
            "Should match exact format and hour 1");
        Assert.That(func(new OnlyNullableTimeSpanEntity { Duration = null }), Is.False,
            "Should not match when null");
    }

    private record OnlyOneDateTimeOffsetEntity
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    private record OnlyNullableDateTimeOffsetEntity
    {
        public DateTimeOffset? CreatedAt { get; set; }
    }

    [Test]
    public void BindSearch_WithAnd_DateTimeOffset_YearAndExactDate()
    {
        var func = GetTestFuncWithAndSearch<OnlyOneDateTimeOffsetEntity>("2023", "2023-01-01");

        Assert.That(func(new OnlyOneDateTimeOffsetEntity { CreatedAt = new DateTimeOffset(2023, 1, 1, 10, 0, 0, TimeSpan.FromHours(2)) }), Is.True,
            "Should match: year 2023 and exact date 2023-01-01");
        Assert.That(func(new OnlyOneDateTimeOffsetEntity { CreatedAt = new DateTimeOffset(2022, 12, 31, 23, 0, 0, TimeSpan.FromHours(1)) }), Is.False,
            "Should not match: year/date do not satisfy both terms");
    }

    [Test]
    public void BindSearch_WithAnd_NullableDateTimeOffset_ExactDate()
    {
        var func = GetTestFuncWithAndSearch<OnlyNullableDateTimeOffsetEntity>("2023", "2023-01-01");

        Assert.That(func(new OnlyNullableDateTimeOffsetEntity { CreatedAt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) }), Is.True,
            "Should match when nullable has value meeting both terms");
        Assert.That(func(new OnlyNullableDateTimeOffsetEntity { CreatedAt = null }), Is.False,
            "Should not match when null");
    }
}