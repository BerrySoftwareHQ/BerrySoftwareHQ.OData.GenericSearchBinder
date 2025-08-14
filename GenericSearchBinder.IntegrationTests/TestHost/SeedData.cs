namespace BerrySoftwareHQ.OData.GenericSearchBinder.IntegrationTests.TestHost;

public static class SeedData
{
    public static IEnumerable<Product> DefaultProducts()
    {
        var contoso = new Company { Id = 1, Name = "Contoso" };
        var fabrikam = new Company { Id = 2, Name = "Fabrikam" };
        return new List<Product>
        {
            new() { Id = 1, Name = "Apple", Category = "Fruit", Description = "Green apple", Price = 1.2m, CreatedOn = new DateTime(2024, 1, 15), AvailableOn = new DateOnly(2024, 1, 15), AvailableAt = new TimeOnly(13, 5, 0), IsFeatured = true, Company = contoso, Offices = { new Office{ Id = 101, Name = "Contoso HQ" } } },
            new() { Id = 2, Name = "Banana", Category = "Fruit", Description = "Ripe banana", Price = 0.8m, CreatedOn = new DateTime(2024, 2, 10), AvailableOn = new DateOnly(2024, 2, 10), AvailableAt = new TimeOnly(12, 30, 0), IsFeatured = false, Company = fabrikam, Offices = { new Office{ Id = 201, Name = "MegaCorp Warehouse" } } },
            new() { Id = 3, Name = "Cherry", Category = "Fruit", Description = "Red cherries", Price = 2.5m, CreatedOn = new DateTime(2024, 3, 5), AvailableOn = new DateOnly(2024, 3, 5), AvailableAt = new TimeOnly(13, 0, 0), IsFeatured = true, Company = contoso },
            new() { Id = 4, Name = "Ball", Category = "Toy", Description = "Blue ball", Price = 5.0m, CreatedOn = new DateTime(2023, 12, 25), AvailableOn = new DateOnly(2023, 12, 25), AvailableAt = new TimeOnly(8, 45, 0), IsFeatured = false },
            new() { Id = 5, Name = "Desk2", Category = "Furniture", Description = null, Price = 120.0m, CreatedOn = new DateTime(2022, 8, 1), AvailableOn = new DateOnly(2022, 8, 1), AvailableAt = new TimeOnly(9, 15, 0), IsFeatured = false, Offices = { new Office{ Id = 501, Name = "Branch MegaCorp" } } },
            new() { Id = 6, Name = null, Category = "Misc", Description = "Unknown item 123", Price = 9.99m, CreatedOn = new DateTime(2021, 5, 17), AvailableOn = new DateOnly(2021, 5, 17), AvailableAt = new TimeOnly(17, 20, 0), IsFeatured = true },
        };
    }
}
