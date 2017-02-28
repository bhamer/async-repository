using AsyncRepository.Models;
using System.Data.Entity;

namespace AsyncRepository.UnitsOfWork
{
    public class MyDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Trade> Trades { get; set; }

        public MyDbContext(string connectionString) 
            : base(connectionString) { }
            
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<MyDbContext>(null);
        }
    }

}
