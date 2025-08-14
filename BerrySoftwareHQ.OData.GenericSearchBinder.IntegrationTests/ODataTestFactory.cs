using BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests;

public sealed class ODataTestFactory : WebApplicationFactory<ProgramStub>
{
    private readonly IEnumerable<TestHost.Product>? _seed;

    public ODataTestFactory(IEnumerable<TestHost.Product>? seed = null)
    {
        _seed = seed;
    }

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
            services.AddControllers().AddOData(opt =>
            {
                opt.Select().Filter().OrderBy().Count().Expand().SetMaxTop(100)
                   .AddRouteComponents("odata", GetEdmModel(), routeServices =>
                   {
                       // Register our custom search binder within OData per-route services
                       routeServices.AddSingleton<ISearchBinder, global::BerrySoftwareHQ.OData.GenericSearchBinder.GenericSearchBinder>();
                   });
            });

            // Disable automatic 400 for model state errors to avoid test noise
            services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);

            // Register our in-memory data source (seed if provided)
            if (_seed is not null)
                services.AddSingleton(new ProductsRepository(_seed));
            else
                services.AddSingleton(new ProductsRepository());
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        });
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
