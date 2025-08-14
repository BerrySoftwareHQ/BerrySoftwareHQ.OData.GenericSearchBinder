using BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests.TestHost;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests.Controllers;

public class ProductsController(ProductsRepository repo) : ODataController
{
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
    public IQueryable<Product> Get()
    {
        // Return an IQueryable to ensure OData applies $search via our GenericSearchBinder
        return repo.Products.AsQueryable();
    }
}
