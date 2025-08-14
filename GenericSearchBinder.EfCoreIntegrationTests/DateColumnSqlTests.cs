using GenericSearchBinder.EfCoreIntegrationTests.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests;

[Category("Integration.EF")] 
public class DateColumnSqlTests
{
    private EfODataTestFactory _factory = null!;
    private SqlCaptureInterceptor _sql = null!;

    [TearDown]
    public void TearDown()
    {
        _factory?.Dispose();
    }

    private async Task GetAsync(string url)
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
    }

    private static IEnumerable<string> ProductSelects(IEnumerable<string> commands)
        => commands.Where(c =>
            c.Contains("select", StringComparison.OrdinalIgnoreCase) &&
            c.Contains("products", StringComparison.OrdinalIgnoreCase));

    [Test]
    public async Task Search_DateTime_year_only_references_CreatedOn_in_SQL()
    {
        _factory = new EfODataTestFactory(db =>
        {
            db.Products.AddRange(
                new Product { Id = 1, Name = "A", CreatedOn = new DateTime(2024,1,1), AvailableOn = new DateOnly(1999,1,1) },
                new Product { Id = 2, Name = "B", CreatedOn = new DateTime(2024,6,1), AvailableOn = new DateOnly(1999,1,1) }
            );
        });
        _sql = _factory.Services.GetRequiredService<SqlCaptureInterceptor>();
        _sql.Reset();

        await GetAsync("/odata/Products?$search=%222024%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        var whereSql = selects.FirstOrDefault(s => s.Contains("where", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        Assert.That(whereSql, Does.Contain("CreatedOn"));
    }

    [Test]
    public async Task Search_DateOnly_year_only_references_AvailableOn_in_SQL()
    {
        _factory = new EfODataTestFactory(db =>
        {
            db.Products.AddRange(
                new Product { Id = 1, Name = "A", CreatedOn = new DateTime(1999,1,1), AvailableOn = new DateOnly(2024,1,1) },
                new Product { Id = 2, Name = "B", CreatedOn = new DateTime(1999,6,1), AvailableOn = new DateOnly(2024,6,1) }
            );
        });
        _sql = _factory.Services.GetRequiredService<SqlCaptureInterceptor>();
        _sql.Reset();

        await GetAsync("/odata/Products?$search=%222024%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        var whereSql = selects.FirstOrDefault(s => s.Contains("where", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        Assert.That(whereSql, Does.Contain("AvailableOn"));
    }

    [Test]
    public async Task Search_Date_year_when_both_fields_present_references_both_columns_in_SQL()
    {
        _factory = new EfODataTestFactory(db =>
        {
            db.Products.AddRange(
                new Product { Id = 1, Name = "A", CreatedOn = new DateTime(2024,1,1), AvailableOn = new DateOnly(2023,1,1) },
                new Product { Id = 2, Name = "B", CreatedOn = new DateTime(2023,1,1), AvailableOn = new DateOnly(2024,1,1) },
                new Product { Id = 3, Name = "C", CreatedOn = new DateTime(2024,1,1), AvailableOn = new DateOnly(2024,1,1) },
                new Product { Id = 4, Name = "D", CreatedOn = new DateTime(2022,1,1), AvailableOn = new DateOnly(2022,1,1) }
            );
        });
        _sql = _factory.Services.GetRequiredService<SqlCaptureInterceptor>();
        _sql.Reset();

        await GetAsync("/odata/Products?$search=%222024%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        var whereHasCreatedOn = selects.Any(s => s.Contains("where", StringComparison.OrdinalIgnoreCase) && s.Contains("CreatedOn", StringComparison.OrdinalIgnoreCase));
        var whereHasAvailableOn = selects.Any(s => s.Contains("where", StringComparison.OrdinalIgnoreCase) && s.Contains("AvailableOn", StringComparison.OrdinalIgnoreCase));
        Assert.That(whereHasCreatedOn, Is.True, "Expected WHERE to reference CreatedOn");
        Assert.That(whereHasAvailableOn, Is.True, "Expected WHERE to reference AvailableOn");
    }

    [Test]
    public async Task Search_TimeOnly_exact_references_AvailableAt_in_SQL()
    {
        _factory = new EfODataTestFactory(db =>
        {
            db.Products.AddRange(
                new Product { Id = 1, Name = "A", AvailableAt = new TimeOnly(13, 5), CreatedOn = new DateTime(1999,1,1), AvailableOn = new DateOnly(1999,1,1) },
                new Product { Id = 2, Name = "B", AvailableAt = new TimeOnly(12, 0), CreatedOn = new DateTime(1999,1,1), AvailableOn = new DateOnly(1999,1,1) }
            );
        });
        _sql = _factory.Services.GetRequiredService<SqlCaptureInterceptor>();
        _sql.Reset();

        await GetAsync("/odata/Products?$search=%2213:05%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        var whereSql = selects.FirstOrDefault(s => s.Contains("where", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        Assert.That(whereSql, Does.Contain("AvailableAt"));
    }
}
