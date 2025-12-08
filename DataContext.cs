using Microsoft.EntityFrameworkCore;

namespace ArWidgetApi.Data
{
    public class DataContext : ApplicationDbContext
    {
        public DataContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
