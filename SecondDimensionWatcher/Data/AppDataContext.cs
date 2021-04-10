using Microsoft.EntityFrameworkCore;

namespace SecondDimensionWatcher.Data
{
    public class AppDataContext : DbContext
    {
        public AppDataContext(DbContextOptions<AppDataContext> options)
            : base(options)
        {
        }

        public DbSet<AnimationInfo> AnimationInfo { get; set; }
    }
}