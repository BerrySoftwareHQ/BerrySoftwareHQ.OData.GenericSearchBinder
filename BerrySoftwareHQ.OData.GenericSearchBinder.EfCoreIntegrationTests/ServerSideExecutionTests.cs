using Microsoft.Extensions.DependencyInjection;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests;

[Category("Integration.EF")] 
public class ServerSideExecutionTests
{
    private EfODataTestFactory _factory = null!;
    private SqlCaptureInterceptor _sql = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new EfODataTestFactory();
        _sql = _factory.Services.GetRequiredService<SqlCaptureInterceptor>();
        _sql.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    private async Task GetAsync(string url)
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        // We don't parse JSON here; we only need to ensure the request executed to capture SQL
    }

    private static IEnumerable<string> ProductSelects(IEnumerable<string> commands)
        => commands.Where(c =>
            c.Contains("select", StringComparison.OrdinalIgnoreCase) &&
            c.Contains("products", StringComparison.OrdinalIgnoreCase));

    private static bool HasWhere(string sql) => sql.IndexOf("where", StringComparison.OrdinalIgnoreCase) >= 0;
    private static bool HasLikeOrInstr(string sql) => sql.Contains(" like ", StringComparison.OrdinalIgnoreCase) || sql.Contains(" instr(", StringComparison.OrdinalIgnoreCase);

    [Test]
    public async Task Search_String_UsesWhereAndLike_ServerSide()
    {
        await GetAsync("/odata/Products?$search=a");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty, "No SQL SELECT for Products captured");
        Assert.That(selects.Any(HasWhere), Is.True, "Expected WHERE for server-side filtering");
        Assert.That(selects.Any(HasLikeOrInstr), Is.True, "Expected LIKE/INSTR for substring search");
    }

    [Test]
    public async Task Search_And_UsesWhereAndLike_ServerSide()
    {
        await GetAsync("/odata/Products?$search=a AND b");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        Assert.That(selects.Any(HasWhere), Is.True);
        Assert.That(selects.Any(HasLikeOrInstr), Is.True);
    }

    [Test]
    public async Task Search_Or_UsesWhereAndLike_ServerSide()
    {
        await GetAsync("/odata/Products?$search=a OR ch");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        Assert.That(selects.Any(HasWhere), Is.True);
        Assert.That(selects.Any(HasLikeOrInstr), Is.True);
    }

    [Test]
    public async Task Search_Not_UsesWhere_ServerSide()
    {
        await GetAsync("/odata/Products?$search=NOT a");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        Assert.That(selects.Any(HasWhere), Is.True, "Expected WHERE for NOT search");
        // LIKE may still appear (NOT LIKE), but we don't require it explicitly here to keep it robust
    }

    [Test]
    public async Task Search_NumericSubstring_UsesWhereAndLike_ServerSide()
    {
        await GetAsync("/odata/Products?$search=%2230%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty);
        Assert.That(selects.Any(HasWhere), Is.True);
        Assert.That(selects.Any(HasLikeOrInstr), Is.True, "Expected LIKE/INSTR for numeric substring (e.g., '30' in 300)");
    }

    [Test]
    public async Task Search_DateTimeYear_ServerSideWhere()
    {
        await GetAsync("/odata/Products?$search=%222024%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty, "No SQL SELECT for Products captured");
        Assert.That(selects.Any(HasWhere), Is.True, "Expected WHERE for server-side DateTime year filtering");
    }

    [Test]
    public async Task Search_ExactDate_ServerSideWhere()
    {
        await GetAsync("/odata/Products?$search=%222024-01-15%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty, "No SQL SELECT for Products captured");
        Assert.That(selects.Any(HasWhere), Is.True, "Expected WHERE for server-side exact Date filtering");
    }

    [Test]
    public async Task Search_ExactDateOnly_ServerSideWhere()
    {
        await GetAsync("/odata/Products?$search=%222024-01-15%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty, "No SQL SELECT for Products captured");
        Assert.That(selects.Any(HasWhere), Is.True, "Expected WHERE for server-side exact DateOnly filtering");
    }

    [Test]
    public async Task Search_ExactTimeOnly_ServerSideWhere()
    {
        await GetAsync("/odata/Products?$search=%2213:05%22");

        var selects = ProductSelects(_sql.Commands).ToList();
        Assert.That(selects, Is.Not.Empty, "No SQL SELECT for Products captured");
        Assert.That(selects.Any(HasWhere), Is.True, "Expected WHERE for server-side exact TimeOnly filtering");
    }
}
