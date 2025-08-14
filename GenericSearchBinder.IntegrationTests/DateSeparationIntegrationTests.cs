using System.Text.Json;
using BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests.TestHost;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests;

public class DateSeparationIntegrationTests
{
    private ODataTestFactory _factory = null!;

    [TearDown]
    public void TearDown()
    {
        _factory?.Dispose();
    }

    private static async Task<JsonElement> GetODataValueArrayAsync(ODataTestFactory factory, string url)
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.That(doc.RootElement.TryGetProperty("value", out var value), Is.True, "OData payload does not contain 'value'");
        return value.Clone();
    }

    [Test]
    public async Task Search_DateTimeYear_only_matches_DateTime_not_DateOnly()
    {
        var data = new[]
        {
            new Product { Id = 1, Name = "A", CreatedOn = new DateTime(2024,1,15), AvailableOn = new DateOnly(1999,1,1) },
            new Product { Id = 2, Name = "B", CreatedOn = new DateTime(2024,5,1),  AvailableOn = new DateOnly(2000,1,1) },
            new Product { Id = 3, Name = "C", CreatedOn = new DateTime(2023,1,1),  AvailableOn = new DateOnly(1999,1,1) },
        };
        _factory = new ODataTestFactory(data);

        var value = await GetODataValueArrayAsync(_factory, "/odata/Products?$search=%222024%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task Search_DateOnlyYear_only_matches_DateOnly_not_DateTime()
    {
        var data = new[]
        {
            new Product { Id = 1, Name = "A", CreatedOn = new DateTime(1999,1,1), AvailableOn = new DateOnly(2024,1,1) },
            new Product { Id = 2, Name = "B", CreatedOn = new DateTime(2000,1,1), AvailableOn = new DateOnly(2024,6,1) },
            new Product { Id = 3, Name = "C", CreatedOn = new DateTime(1998,1,1), AvailableOn = new DateOnly(1999,1,1) },
        };
        _factory = new ODataTestFactory(data);

        var value = await GetODataValueArrayAsync(_factory, "/odata/Products?$search=%222024%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task Search_TimeOnly_exact_time_matches_only_time()
    {
        var data = new[]
        {
            new Product { Id = 1, Name = "A", AvailableAt = new TimeOnly(13, 5), CreatedOn = new DateTime(1999,1,1), AvailableOn = new DateOnly(1999,1,1) },
            new Product { Id = 2, Name = "B", AvailableAt = new TimeOnly(12, 0), CreatedOn = new DateTime(1999,1,1), AvailableOn = new DateOnly(1999,1,1) },
        };
        _factory = new ODataTestFactory(data);

        var value = await GetODataValueArrayAsync(_factory, "/odata/Products?$search=%2213:05%22");
        var ids = value.EnumerateArray().Select(e => e.GetProperty("Id").GetInt32()).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { 1 }));
    }
}
