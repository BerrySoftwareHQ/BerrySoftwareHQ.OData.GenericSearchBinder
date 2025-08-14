using Microsoft.EntityFrameworkCore;

namespace BerrySoftwareHQ.OData.GenericSearchBinder.EfCoreIntegrationTests.TestHost;

public class ProductsDbContext(DbContextOptions<ProductsDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Office> Offices => Set<Office>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.CompanyId);

        modelBuilder.Entity<Office>()
            .HasOne(o => o.Product)
            .WithMany(p => p.Offices)
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}