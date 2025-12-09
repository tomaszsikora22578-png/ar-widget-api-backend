using Microsoft.EntityFrameworkCore;

namespace ArWidgetApi
{
    public class DataContext : ApplicationDbContext
    {
        public DataContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
