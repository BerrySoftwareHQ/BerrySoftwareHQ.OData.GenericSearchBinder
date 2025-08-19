using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.Tests;

// example SearchClause with OR {"Expression":{"OperatorKind":0,"Left":{"Text":"Test","TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":24},"Right":{"Text":"TestT","TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":24},"TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":4}}
// example SearchClause with single search {"Expression":{"Text":"Test","TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":24}}
// example SearchClause with AND {"Expression":{"OperatorKind":1,"Left":{"Text":"Test","TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":24},"Right":{"Text":"TestT","TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":24},"TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":4}}
// example SearchClause with NOT {"Expression":{"OperatorKind":1,"Operand":{"Text":"Test","TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":24},"TypeReference":{"IsNullable":false,"Definition":{"Name":"Boolean","Namespace":"Edm","TypeKind":1,"PrimitiveKind":2,"SchemaElementKind":1,"FullName":"Edm.Boolean"}},"Kind":5}}

public class SingleSearchTests
{
    private static Func<T, bool> GetTestFunc<T>(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return _ => true;
        }
        var binder = new GenericSearchBinder();
        var searchTermNode = new SearchTermNode(searchTerm);
        var searchClause = new SearchClause(searchTermNode);
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
    public void BindSearch_WithSimpleTerm_CreatesCorrectFilterExpression()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("a");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match 'Apple'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Banana" }), Is.True, "Should match 'Banana'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "BANANA" }), Is.True,
            "Should match 'BANANA' case-insensitively");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "Cherry" }), Is.False, "Should not match 'Cherry'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithNumericStringSearchValue()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("123");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "123" }), Is.True, "Should match '123'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "abc123" }), Is.True, "Should match 'abc123'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "456" }), Is.False, "Should not match '456'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithStringAndNumericStringSearchValue()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("b1");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "b1" }), Is.True, "Should match 'b1'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "ab1c" }), Is.True, "Should match 'ab1c'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "b2" }), Is.False, "Should not match 'b2'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.False, "Should not match null property");
    }

    [Test]
    public void BindSearch_WithSpecialCharacters_ReturnsCorrectExpression()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("!@#");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "!@#test" }), Is.True, "Should match '!@#test'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test!@#" }), Is.True, "Should match 'test!@#'");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "normal" }), Is.False, "Should not match 'normal'");
    }

    [Test]
    public void BindSearch_WithHyphenatedTerm_ReturnsCorrectExpression()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("test-with-dash");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "test-with-dash" }), Is.True, "Should match exact hyphenated term");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "prefix test-with-dash suffix" }), Is.True, "Should match hyphenated term as substring");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "test with dash" }), Is.False, "Should not match when hyphens are missing");
    }

    [Test]
    public void BindSearch_WithEmailLikeTerm_ReturnsCorrectExpression()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("email-like@example.com");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "email-like@example.com" }), Is.True, "Should match exact email-like term");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "contact: email-like@example.com (primary)" }), Is.True, "Should match email-like term as substring");
        Assert.That(func(new OnlyOneStringTestEntity { Name = "email_like@example_com" }), Is.False, "Should not match different separators");
    }

    [Test]
    public void BindSearch_WithEmptyTerm_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFunc<OnlyOneStringTestEntity>("");

        Assert.That(func(new OnlyOneStringTestEntity { Name = "Apple" }), Is.True, "Should match any string");
        Assert.That(func(new OnlyOneStringTestEntity { Name = null }), Is.True, "Should match null property");
    }

    private record OnlyOneBooleanTestEntity
    {
        public bool IsActive { get; set; }
    }

    [Test]
    public void BindSearch_WithNonStringProperty_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFunc<OnlyOneBooleanTestEntity>("true");

        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = true }), Is.True, "Should match true");
        Assert.That(func(new OnlyOneBooleanTestEntity { IsActive = false }), Is.False, "Should match false");
    }

    private record OnlyOneIntTestEntity
    {
        public int Age { get; set; }
    }

    [Test]
    public void BindSearch_WithNonStringPropertyAndNumericSearchValue()
    {
        var func = GetTestFunc<OnlyOneIntTestEntity>("30");

        Assert.That(func(new OnlyOneIntTestEntity { Age = 30 }), Is.True, "Should match 30");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 130 }), Is.True, "Should match 130");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 300 }), Is.True, "Should match 300");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 25 }), Is.False, "Should not match 25");
        Assert.That(func(new OnlyOneIntTestEntity { Age = 35 }), Is.False, "Should not match 35");
    }

    private record OneIntOneStringTestEntity
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithMixedProperties_SearchInt_ReturnsCorrectExpression()
    {
        var func = GetTestFunc<OneIntOneStringTestEntity>("30");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Alice" }), Is.True, "Should match Age 30");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Bob" }), Is.False,
            "Should not match Age 25");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.True,
            "Should match Age 30 with null Name");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "Charlie" }), Is.False,
            "Should not match Age 35");
    }

    [Test]
    public void BindSearch_WithMixedProperties_SearchString_ReturnsCorrectExpression()
    {
        var func = GetTestFunc<OneIntOneStringTestEntity>("Alice");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "Alice" }), Is.True,
            "Should match Name 'Alice'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "Bob" }), Is.False,
            "Should not match Name 'Bob'");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.False,
            "Should not match null Name");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "Charlie" }), Is.False,
            "Should not match Name 'Charlie'");
    }

    [Test]
    public void BindSearch_WithMixedProperties_SearchStringAndInt_ReturnsCorrectExpression()
    {
        var func = GetTestFunc<OneIntOneStringTestEntity>("3");

        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = "40" }), Is.True,
            "Should match Name '40' and Age 30");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 25, Name = "4" }), Is.False,
            "Should not match Name '4' and Age 25");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 30, Name = null }), Is.True,
            "Should match null Name with Age 30");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 35, Name = "30" }), Is.True,
            "Should match Name '30' and Age 35");
        Assert.That(func(new OneIntOneStringTestEntity { Age = 2, Name = "30" }), Is.True,
            "Should match Name '30' and Age 2");
    }

    private record OnlyOneDateTimeTestEntity
    {
        public DateTime CreatedDate { get; set; }
    }

    [Test]
    public void BindSearch_WithDateTimeProperty_ReturnsAlwaysTrueExpression()
    {
        var func = GetTestFunc<OnlyOneDateTimeTestEntity>("2023");

        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2023, 1, 1) }), Is.True,
            "Should match 2023 date");
        Assert.That(func(new OnlyOneDateTimeTestEntity { CreatedDate = new DateTime(2022, 1, 1) }), Is.False,
            "Shouldn't match 2022 date");
    }

    private record DateTimeAndStringTestEntity
    {
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
    }

    [Test]
    public void BindSearch_WithDateTimeAndStringProperty_SearchesOnlyString()
    {
        var func = GetTestFunc<DateTimeAndStringTestEntity>("2023");

        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "Created in 2023" }), Is.True,
            "Should match '2023' in description");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 1), Description = "Old record" }), Is.True,
            "Should match when year contains '2023'");
        Assert.That(
            func(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2022, 1, 1), Description = "Created in 2023" }), Is.True,
            "Should match '2023' in description regardless of actual date");
        Assert.That(
            func(new DateTimeAndStringTestEntity { CreatedDate = new DateTime(2022, 1, 1), Description = null }),
            Is.False, "Should not match null description");
    }

    [Test]
    public void BindSearch_WithDateComponents_SearchesCorrectly()
    {
        // Test with day number
        var funcDay = GetTestFunc<DateTimeAndStringTestEntity>("15");
        Assert.That(
            funcDay(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event on day 15" }), Is.True,
            "Should match '15' in description");
        Assert.That(
            funcDay(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Different day" }), Is.True,
            "Should match when the day contains '15'");

        // Test with month number
        var funcMonth = GetTestFunc<DateTimeAndStringTestEntity>("1");
        Assert.That(
            funcMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event in month 1" }), Is.True,
            "Should match '1' in description");
        Assert.That(
            funcMonth(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Different month" }), Is.True,
            "Should match when month contains '1'");

        // Test with year
        var funcYear = GetTestFunc<DateTimeAndStringTestEntity>("2023");
        Assert.That(
            funcYear(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2023, 1, 15), Description = "Event in 2023" }), Is.True,
            "Should match '2023' in description");
        Assert.That(
            funcYear(new DateTimeAndStringTestEntity
                { CreatedDate = new DateTime(2022, 1, 15), Description = "Different year" }), Is.False,
            "Should not match when description doesn't contain '2023'");
    }

    private record EntityWithNullableDateTime
    {
        public DateTime? OptionalDate { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithNullableDateTimeProperty_HandlesNullCorrectly()
    {
        var func = GetTestFunc<EntityWithNullableDateTime>("2023");

        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = new DateTime(2023, 1, 1), Name = "Event in 2023" }),
            Is.True, "Should match '2023' in name");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = "Event in 2023" }), Is.True,
            "Should match '2023' in name when date is null");
        Assert.That(
            func(new EntityWithNullableDateTime { OptionalDate = null, Name = "Old event" }),
            Is.False, "Should not match when name doesn't contain '2023'");
        Assert.That(func(new EntityWithNullableDateTime { OptionalDate = null, Name = null }), Is.False,
            "Should not match when both properties are null");
    }

    // -------------------- DateOnly tests --------------------
    private record OnlyOneDateOnlyTestEntity
    {
        public DateOnly Date { get; set; }
    }

    [Test]
    public void BindSearch_WithDateOnlyProperty_SearchesCorrectly()
    {
        var func = GetTestFunc<OnlyOneDateOnlyTestEntity>("2023");
        Assert.That(func(new OnlyOneDateOnlyTestEntity { Date = new DateOnly(2023, 1, 1) }), Is.True,
            "Should match year 2023");
        Assert.That(func(new OnlyOneDateOnlyTestEntity { Date = new DateOnly(2022, 1, 1) }), Is.False,
            "Shouldn't match year 2022");

        var funcExact = GetTestFunc<OnlyOneDateOnlyTestEntity>("2023-08-14");
        Assert.That(funcExact(new OnlyOneDateOnlyTestEntity { Date = new DateOnly(2023, 8, 14) }), Is.True,
            "Should match exact date 2023-08-14");
        Assert.That(funcExact(new OnlyOneDateOnlyTestEntity { Date = new DateOnly(2023, 8, 13) }), Is.False,
            "Shouldn't match different date");
    }

    private record EntityWithNullableDateOnly
    {
        public DateOnly? OptionalDate { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithNullableDateOnlyProperty_HandlesNullCorrectly()
    {
        var func = GetTestFunc<EntityWithNullableDateOnly>("2023");
        Assert.That(func(new EntityWithNullableDateOnly { OptionalDate = new DateOnly(2023, 1, 1), Name = "Event" }), Is.True);
        Assert.That(func(new EntityWithNullableDateOnly { OptionalDate = null, Name = "Event 2023" }), Is.True);
        Assert.That(func(new EntityWithNullableDateOnly { OptionalDate = null, Name = "Event" }), Is.False);
    }

    // -------------------- TimeOnly tests --------------------
    private record OnlyOneTimeOnlyTestEntity
    {
        public TimeOnly Time { get; set; }
    }

    [Test]
    public void BindSearch_WithTimeOnlyProperty_SearchesCorrectly()
    {
        var funcHour = GetTestFunc<OnlyOneTimeOnlyTestEntity>("13");
        Assert.That(funcHour(new OnlyOneTimeOnlyTestEntity { Time = new TimeOnly(13, 5, 0) }), Is.True,
            "Should match hour 13");
        Assert.That(funcHour(new OnlyOneTimeOnlyTestEntity { Time = new TimeOnly(12, 30, 0) }), Is.False,
            "Shouldn't match hour 12");

        var funcExact = GetTestFunc<OnlyOneTimeOnlyTestEntity>("13:05");
        Assert.That(funcExact(new OnlyOneTimeOnlyTestEntity { Time = new TimeOnly(13, 5, 0) }), Is.True,
            "Should match exact time HH:mm");
        Assert.That(funcExact(new OnlyOneTimeOnlyTestEntity { Time = new TimeOnly(13, 6, 0) }), Is.False,
            "Shouldn't match different minute");
    }

    private record EntityWithNullableTimeOnly
    {
        public TimeOnly? OptionalTime { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void BindSearch_WithNullableTimeOnlyProperty_HandlesNullCorrectly()
    {
        var func = GetTestFunc<EntityWithNullableTimeOnly>("13");
        Assert.That(func(new EntityWithNullableTimeOnly { OptionalTime = new TimeOnly(13, 0), Name = "X" }), Is.True);
        Assert.That(func(new EntityWithNullableTimeOnly { OptionalTime = null, Name = "13 o'clock" }), Is.True);
        Assert.That(func(new EntityWithNullableTimeOnly { OptionalTime = null, Name = "X" }), Is.False);
    }
}