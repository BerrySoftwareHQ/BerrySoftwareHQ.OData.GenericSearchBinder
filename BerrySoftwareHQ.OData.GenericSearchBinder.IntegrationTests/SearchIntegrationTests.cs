using System.Text.Json;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests;

public class SearchIntegrationTests
{
    private ODataTestFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ODataTestFactory();
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
        Assert.That(doc.RootElement.TryGetProperty("value", out var value), Is.True, "OData payload does not contain 'value'");
        return value.Clone();
    }

    [Test]
    public async Task Search_SimpleTerm_FiltersAcrossStringProps()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=a");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(new[] {"Apple", "Banana", "Ball"}, Is.SubsetOf(names));
        Assert.That(names.Contains("Cherry"), Is.False);
    }

    [Test]
    public async Task Search_IsCaseInsensitive()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=BANANA");
        var names = value.EnumerateArray().Select(e => e.TryGetProperty("Name", out var n) ? n.GetString() : null).ToList();
        Assert.That(names, Does.Contain("Banana"));
    }

    [Test]
    public async Task Search_AndOperator_MatchesWhenBothTermsPresent()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=a AND b");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(new[] {"Banana", "Ball"}, Is.SubsetOf(names));
        Assert.That(names.Contains("Apple"), Is.False);
    }

    [Test]
    public async Task Search_NumericString_MatchesWithinText()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22123%22");
        var descriptions = value.EnumerateArray().Select(e => e.TryGetProperty("Description", out var d) ? d.GetString() : null).ToList();
        Assert.That(descriptions.Any(d => d != null && d.Contains("123", StringComparison.OrdinalIgnoreCase)), Is.True);
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
    public async Task Search_MatchesNumericProperties_ByToString()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22120%22");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Does.Contain("Desk2"));
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
    public async Task Search_MatchesDateTimeYear_WithInvariantToString()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Does.Contain(1));
        Assert.That(ids, Does.Contain(2));
        Assert.That(ids, Does.Contain(3));
    }

    [Test]
    public async Task Search_NullStringProperty_DoesNotThrow_StillMatchesOtherProps()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22123%22");
        var names = value.EnumerateArray().Select(e => e.TryGetProperty("Name", out var n) ? n.GetString() : null).ToList();
        Assert.That(names, Does.Contain(null));
    }

    [Test]
    public async Task Search_DoesNotInclude_ReferenceNavigation_CompanyName()
    {
        // "Contoso" only appears in Company.Name for Apple/Cherry, not on Product scalar fields.
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22Contoso%22");
        var names = value.EnumerateArray().Select(e => e.TryGetProperty("Name", out var n) ? n.GetString() : null).ToList();
        // Should NOT match anything solely due to navigation values
        Assert.That(names, Is.Empty);
    }

    [Test]
    public async Task Search_DoesNotInclude_CollectionNavigation_OfficeName()
    {
        // "MegaCorp" only appears in Offices.Name for Banana and Desk2
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%22MegaCorp%22");
        var names = value.EnumerateArray().Select(e => e.TryGetProperty("Name", out var n) ? n.GetString() : null).ToList();
        Assert.That(names, Is.Empty);
    }

    [Test]
    public async Task Search_MatchesDateOnlyYear_ReturnsExpectedRows()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Does.Contain(1));
        Assert.That(ids, Does.Contain(2));
        Assert.That(ids, Does.Contain(3));
    }

    [Test]
    public async Task Search_MatchesExactDateOnly_ReturnsExpectedRow()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%222024-01-15%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task Search_MatchesTimeOnlyHour_ReturnsExpectedRows()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%2213%22");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Does.Contain("Apple")); // 13:05
        Assert.That(names, Does.Contain("Cherry")); // 13:00
        Assert.That(names, Does.Not.Contain("Banana")); // 12:30
    }

    [Test]
    public async Task Search_MatchesExactTimeOnly_ReturnsExpectedRow()
    {
        var value = await GetODataValueArrayAsync("/odata/Products?$search=%2213:05%22");
        var names = value.EnumerateArray().Select(e => e.GetProperty("Name").GetString()).ToList();
        Assert.That(names, Is.EquivalentTo(new[] { "Apple" }));
    }
    
    private static async Task<JsonElement> GetODataValueArrayAsync(ODataTestFactory factory, string url)
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.GetProperty("value").Clone();
    }

    private static List<string?> GetStrings(JsonElement value, string property)
    {
        return value.EnumerateArray()
            .Select(e => e.TryGetProperty(property, out var p) ? p.GetString() : null)
            .ToList();
    }

    [Test]
    public async Task Search_SpecialCharacters_Hyphen_Quoted_Matches_NameAndDescription()
    {
        // Arrange: custom factory seed with hyphenated term
        var seed = new[]
        {
            new TestHost.Product { Id = 999, Name = "test-with-dash", Category = "Spec", Description = "contains-hyphen" }
        };
        using var factory = new ODataTestFactory(seed);

        // Act - quoted hyphenated term, should match both Name and Description
        var value = await GetODataValueArrayAsync(factory, "/odata/Products?$search=%22test-with-dash%22");
        var names = GetStrings(value, "Name");

        // Assert
        Assert.That(names, Does.Contain("test-with-dash"));
    }

    [Test]
    public async Task Search_SpecialCharacters_Hyphen_Quoted_Matches()
    {
        var seed = new[]
        {
            new TestHost.Product { Id = 1000, Name = "email-like@example.com", Category = "Spec", Description = "test-with-dash and underscore_case" }
        };
        using var factory = new ODataTestFactory(seed);

        // Quoted exact hyphenated phrase inside Description
        var value1 = await GetODataValueArrayAsync(factory, "/odata/Products?$search=%22test-with-dash%22");
        var descriptions = GetStrings(value1, "Description");
        Assert.That(descriptions.Any(d => d != null && d.Contains("test-with-dash", StringComparison.OrdinalIgnoreCase)), Is.True);

        // Also verify email-like content with special characters works when quoted
        var value2 = await GetODataValueArrayAsync(factory, "/odata/Products?$search=%22email-like@example.com%22");
        var names2 = GetStrings(value2, "Name");
        Assert.That(names2, Does.Contain("email-like@example.com"));
    }
}
