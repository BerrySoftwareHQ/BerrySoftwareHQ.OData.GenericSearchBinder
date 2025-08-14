using System.Data.Common;
using BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests;

public sealed class EfODataTestFactory(Action<ProductsDbContext>? seed = null)
    : WebApplicationFactory<ProgramStub>
{
    private DbConnection? _connection;

    protected override IHostBuilder? CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseTestServer();
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Single shared in-memory SQLite connection for the app lifetime
            var sqlite = new SqliteConnection("DataSource=:memory:;Cache=Shared");
            sqlite.Open();
            _connection = sqlite;

            // Capture SQL commands
            services.AddSingleton<SqlCaptureInterceptor>();
            services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>(sp => sp.GetRequiredService<SqlCaptureInterceptor>());

            services.AddDbContext<ProductsDbContext>((sp, o) =>
            {
                o.UseSqlite(sqlite);
                o.AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>());
            });

            services.AddControllers().AddOData(opt =>
            {
                opt.Select().Filter().OrderBy().Count().Expand().SetMaxTop(100)
                   .AddRouteComponents("odata", GetEdmModel(), routeServices =>
                   {
                       routeServices.AddSingleton<ISearchBinder, GenericSearchBinder>();
                   });
            });

            services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);

            // Build the provider to seed the DB
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            if (seed is not null)
            {
                seed(db);
                db.SaveChanges();
            }
            else
            {
                SeedDefault(db);
            }
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        });
    }

    private static void SeedDefault(ProductsDbContext db)
    {
        if (db.Products.Any()) return;
        var contoso = new Company { Id = 1, Name = "Contoso" };
        var fabrikam = new Company { Id = 2, Name = "Fabrikam" };
        db.Companies.AddRange(contoso, fabrikam);

        db.Products.AddRange(new[]
        {
            new Product { Id = 1, Name = "Apple", Category = "Fruit", Description = "Green apple", Price = 1.2m, CreatedOn = new DateTime(2024, 1, 15), AvailableOn = new DateOnly(2024, 1, 15), AvailableAt = new TimeOnly(13, 5, 0), IsFeatured = true, CompanyId = contoso.Id },
            new Product { Id = 2, Name = "Banana", Category = "Fruit", Description = "Ripe banana", Price = 0.8m, CreatedOn = new DateTime(2024, 2, 10), AvailableOn = new DateOnly(2024, 2, 10), AvailableAt = new TimeOnly(12, 30, 0), IsFeatured = false, CompanyId = fabrikam.Id },
            new Product { Id = 3, Name = "Cherry", Category = "Fruit", Description = "Red cherries", Price = 2.5m, CreatedOn = new DateTime(2024, 3, 5), AvailableOn = new DateOnly(2024, 3, 5), AvailableAt = new TimeOnly(13, 0, 0), IsFeatured = true, CompanyId = contoso.Id },
            new Product { Id = 4, Name = "Ball", Category = "Toy", Description = "Blue ball", Price = 5.0m, CreatedOn = new DateTime(2023, 12, 25), AvailableOn = new DateOnly(2023, 12, 25), AvailableAt = new TimeOnly(8, 45, 0), IsFeatured = false },
            new Product { Id = 5, Name = "Desk2", Category = "Furniture", Description = null, Price = 120.0m, CreatedOn = new DateTime(2022, 8, 1), AvailableOn = new DateOnly(2022, 8, 1), AvailableAt = new TimeOnly(9, 15, 0), IsFeatured = false },
            new Product { Id = 6, Name = null, Category = "Misc", Description = "Unknown item 123", Price = 9.99m, CreatedOn = new DateTime(2021, 5, 17), AvailableOn = new DateOnly(2021, 5, 17), AvailableAt = new TimeOnly(17, 20, 0), IsFeatured = true },
        });
        db.SaveChanges();

        db.Offices.AddRange(new[]
        {
            new Office { Id = 101, Name = "Contoso HQ", ProductId = 1 },
            new Office { Id = 201, Name = "MegaCorp Warehouse", ProductId = 2 },
            new Office { Id = 501, Name = "Branch MegaCorp", ProductId = 5 },
        });
        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }

    private static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.Namespace = "Test";
        builder.ContainerName = "DefaultContainer";
        builder.EntitySet<Product>("Products");
        return builder.GetEdmModel();
    }
}