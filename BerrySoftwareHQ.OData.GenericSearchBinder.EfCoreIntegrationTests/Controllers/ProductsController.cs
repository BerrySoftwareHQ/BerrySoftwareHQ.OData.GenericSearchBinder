using BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests.TestHost;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests.Controllers;

public class ProductsController(ProductsDbContext db) : ODataController
{
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<Product> Get()
    {
        // Return IQueryable so OData composes $search on EF Core query
        return db.Products;
    }
}