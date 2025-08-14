using System.Text.Json;

namespace GenericSearchBinder.EfCoreIntegrationTests;

[Category("Integration.EF")]
public class SearchEfIntegrationTests
{
    private EfODataTestFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new EfODataTestFactory();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    private async Task<JsonElement> GetODataValueArrayAsync(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.That(doc.RootElement.TryGetProperty("value", out var value), Is.True,
            "OData payload does not contain 'value'");
        return value.Clone();
    }

    [Test]
    public async Task Search_SimpleTerm_FiltersAcrossStringProps()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=a");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(new[] { "Apple", "Banana", "Ball" }, Is.SubsetOf(names));
        Assert.That(names.Contains("Cherry"), Is.False);
    }

    [Test]
    public async Task Search_IsCaseInsensitive()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=BANANA");
        var names = value.EnumerateArray().Select(e => e.TryGetProperty("Name", out var n) ? n.GetString() : null)
            .ToList();
        Assert.That(names, Does.Contain("Banana"));
    }

    [Test]
    public async Task Search_AndOperator_MatchesWhenBothTermsPresent()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=a AND b");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(new[] { "Banana", "Ball" }, Is.SubsetOf(names));
        Assert.That(names.Contains("Apple"), Is.False);
    }

    [Test]
    public async Task Search_OrOperator_ReturnsEitherMatch()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=a OR ch");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Does.Contain("Apple"));
        Assert.That(names, Does.Contain("Banana"));
        Assert.That(names, Does.Contain("Ball"));
        Assert.That(names, Does.Contain("Cherry"));
    }

    [Test]
    public async Task Search_NotOperator_ExcludesMatches()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=NOT a");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Does.Not.Contain("Apple"));
        Assert.That(names, Does.Not.Contain("Banana"));
        Assert.That(names, Does.Not.Contain("Ball"));
    }

    [Test]
    public async Task Search_AndAcrossDifferentProperties_Matches()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=fruit AND ripe");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Does.Contain("Banana"));
    }

    [Test]
    public async Task Search_MatchesBooleanTrue()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=true");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Does.Contain("Apple"));
        Assert.That(names, Does.Contain("Cherry"));
    }

    [Test]
    public async Task Search_MatchesDateTimeYear_ReturnsExpectedRows()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Does.Contain(1));
        Assert.That(ids, Does.Contain(2));
        Assert.That(ids, Does.Contain(3));
        Assert.That(ids, Does.Not.Contain(4));
        Assert.That(ids, Does.Not.Contain(5));
        Assert.That(ids, Does.Not.Contain(6));
    }

    [Test]
    public async Task Search_MatchesExactDate_ReturnsExpectedRow()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024-01-15%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Does.Contain(1));
        Assert.That(ids, Does.Not.Contain(2));
        Assert.That(ids, Does.Not.Contain(3));
        Assert.That(ids, Does.Not.Contain(4));
        Assert.That(ids, Does.Not.Contain(5));
        Assert.That(ids, Does.Not.Contain(6));
    }

    [Test]
    public async Task Search_RespectsOrderingAndCountAndPaging()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/odata/Products?$search=a&$orderby=Name asc&$count=true&$top=2&$skip=0");
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.That(doc.RootElement.TryGetProperty("@odata.count", out var countProp), Is.True);
        var count = countProp.GetInt32();
        Assert.That(count, Is.GreaterThanOrEqualTo(2));
        var value = doc.RootElement.GetProperty("value").EnumerateArray().Select(e => e.GetProperty("Name").GetString())
            .ToList();
        var sorted = value.OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.That(value, Is.EqualTo(sorted));
        Assert.That(value.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Search_DoesNotInclude_ReferenceNavigation_CompanyName()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22Contoso%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.Empty);
    }

    [Test]
    public async Task Search_DoesNotInclude_CollectionNavigation_OfficeName()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22MegaCorp%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.Empty);
    }

    [Test]
    public async Task Search_MatchesDateOnlyYear_ReturnsExpectedRows()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Does.Contain(1));
        Assert.That(ids, Does.Contain(2));
        Assert.That(ids, Does.Contain(3));
        Assert.That(ids, Does.Not.Contain(4));
    }

    [Test]
    public async Task Search_MatchesExactDateOnly_ReturnsExpectedRow()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024-01-15%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { 1 }));
    }

    [Test]
    public async Task Search_MatchesExactTimeOnly_HourBoundary_ReturnsExpectedRow()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%2213:00%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { 3 }));
    }

    [Test]
    public async Task Search_MatchesExactTimeOnly_ReturnsExpectedRow()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%2213:05%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { 1 }));
    }
}