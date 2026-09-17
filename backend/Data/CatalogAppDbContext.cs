using Microsoft.EntityFrameworkCore;
using ServiceCatalogModel = ItServiceManagement.API.ServiceCatalog.Models.ServiceCatalog;

namespace ItServiceManagement.API.Data
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        public DbSet<ServiceCatalogModel> ServiceCatalogs { get; set; }
    }
}