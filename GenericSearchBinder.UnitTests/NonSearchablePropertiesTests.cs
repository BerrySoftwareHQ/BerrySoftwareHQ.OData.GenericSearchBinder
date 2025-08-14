using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace GenericSearchBinder.Tests;

public class NonSearchablePropertiesTests
{
    private static Func<T, bool> GetFuncSingleTerm<T>(string term)
    {
        if (string.IsNullOrEmpty(term)) return _ => true;
        var binder = new GenericSearchBinder();
        var searchClause = new SearchClause(new SearchTermNode(term));
        var builder = new ODataConventionModelBuilder();
        builder.AddEntityType(typeof(T));
        var edmModel = builder.GetEdmModel();
        var context = new QueryBinderContext(edmModel, new ODataQuerySettings(), typeof(T));
        var expression = binder.BindSearch(searchClause, context);
        var lambda = (Expression<Func<T, bool>>)expression;
        return lambda.Compile();
    }

    private static Func<T, bool> GetFuncAndTerms<T>(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return _ => true;
        var binder = new GenericSearchBinder();
        var and = new BinaryOperatorNode(BinaryOperatorKind.And, new SearchTermNode(a), new SearchTermNode(b));
        var searchClause = new SearchClause(and);
        var builder = new ODataConventionModelBuilder();
        builder.AddEntityType(typeof(T));
        var edmModel = builder.GetEdmModel();
        var context = new QueryBinderContext(edmModel, new ODataQuerySettings(), typeof(T));
        var expression = binder.BindSearch(searchClause, context);
        var lambda = (Expression<Func<T, bool>>)expression;
        return lambda.Compile();
    }

    private enum SampleStatus { Unknown = 0, Active = 1, Disabled = 2 }

    private record OnlyEnumEntity
    {
        public SampleStatus Status { get; set; }
    }

    [Test]
    public void EnumProperty_IsIgnored()
    {
        var func = GetFuncSingleTerm<OnlyEnumEntity>("Active");
        Assert.That(func(new OnlyEnumEntity { Status = SampleStatus.Active }), Is.False);
        Assert.That(func(new OnlyEnumEntity { Status = SampleStatus.Disabled }), Is.False);

        var funcNum = GetFuncSingleTerm<OnlyEnumEntity>("1");
        Assert.That(funcNum(new OnlyEnumEntity { Status = SampleStatus.Active }), Is.False);
    }

    private record OnlyByteArrayEntity
    {
        public byte[]? Data { get; set; }
    }

    [Test]
    public void ByteArray_IsIgnored()
    {
        var func = GetFuncSingleTerm<OnlyByteArrayEntity>("abc");
        Assert.That(func(new OnlyByteArrayEntity { Data = new byte[] { 1, 2, 3 } }), Is.False);
        Assert.That(func(new OnlyByteArrayEntity { Data = null }), Is.False);
    }

    private record OnlyObjectEntity
    {
        public object? Tag { get; set; }
    }

    [Test]
    public void Object_IsIgnored()
    {
        var func = GetFuncSingleTerm<OnlyObjectEntity>("foo");
        Assert.That(func(new OnlyObjectEntity { Tag = "foo" }), Is.False, "Binder must not ToString() arbitrary object");
        Assert.That(func(new OnlyObjectEntity { Tag = 123 }), Is.False);
        Assert.That(func(new OnlyObjectEntity { Tag = null }), Is.False);
    }

    private record OnlyUriEntity
    {
        public Uri? Link { get; set; }
    }

    [Test]
    public void Uri_IsIgnored()
    {
        var func = GetFuncSingleTerm<OnlyUriEntity>("http");
        Assert.That(func(new OnlyUriEntity { Link = new Uri("http://example.com") }), Is.False);
        Assert.That(func(new OnlyUriEntity { Link = null }), Is.False);
    }


    private record ComplexAddress
    {
        public string? City { get; set; }
    }

    private record OnlyComplexEntity
    {
        public ComplexAddress? Address { get; set; }
    }

    [Test]
    public void ComplexType_IsIgnored_EvenIfItHasStringInside()
    {
        var func = GetFuncSingleTerm<OnlyComplexEntity>("paris");
        Assert.That(func(new OnlyComplexEntity { Address = new ComplexAddress { City = "Paris" } }), Is.False,
            "Binder should not traverse into complex/nested properties");
        Assert.That(func(new OnlyComplexEntity { Address = null }), Is.False);
    }

    private record WithCollectionProperties
    {
        public List<string>? Tags { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void Collections_AreIgnored_OnlyRootStringsMatch()
    {
        var func = GetFuncAndTerms<WithCollectionProperties>("foo", "bar");
        Assert.That(func(new WithCollectionProperties { Tags = new List<string> { "foo", "bar" }, Name = null }), Is.False,
            "Collection of strings should be ignored");
        Assert.That(func(new WithCollectionProperties { Tags = new List<string> { "foo", "xxx" }, Name = "--foo--bar--" }), Is.True,
            "Should match on Name only");
    }

    private record MixedWithIgnoredTypes
    {
        public Uri? Link { get; set; }
        public TimeSpan? Duration { get; set; }
        public object? Tag { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void MixedEntity_MatchesOnlyOnString()
    {
        var func = GetFuncAndTerms<MixedWithIgnoredTypes>("hello", "123");
        Assert.That(func(new MixedWithIgnoredTypes
        {
            Link = new Uri("http://example.com/hello/123"),
            Duration = TimeSpan.FromMinutes(123),
            Tag = "hello123",
            Name = "--hello--123--"
        }), Is.True);

        Assert.That(func(new MixedWithIgnoredTypes
        {
            Link = new Uri("http://example.com/hello/123"),
            Duration = TimeSpan.FromMinutes(123),
            Tag = "hello123",
            Name = "--hello--"
        }), Is.False);
    }
}
