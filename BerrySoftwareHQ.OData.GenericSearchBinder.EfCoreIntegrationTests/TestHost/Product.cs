namespace BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests.TestHost;

public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateOnly AvailableOn { get; set; }
    public TimeOnly AvailableAt { get; set; }
    public bool IsFeatured { get; set; }

    // Reference navigation (FK)
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    // One-to-many child collection
    public List<Office> Offices { get; set; } = new();
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
}