using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;

namespace RealEstateAPI
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<Property> Properties { get; set; }


    }
}
