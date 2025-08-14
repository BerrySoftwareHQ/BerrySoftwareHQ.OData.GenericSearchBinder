namespace BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests.TestHost;

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

    // Navigation properties (should NOT be searched by default)
    public Company? Company { get; set; }
    public List<Office> Offices { get; set; } = [];
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
}
