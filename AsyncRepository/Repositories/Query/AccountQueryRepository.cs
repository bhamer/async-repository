using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncRepository.Models;
using AsyncRepository.UnitsOfWork;
using System.Data.Entity;

namespace AsyncRepository.Repositories.Query
{
    public class AccountQueryRepository : IAccountQueryRepository
    {
        private readonly string connectionString;

        public AccountQueryRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<List<Account>> GetAccounts()
        {
            using (var context = new MyDbContext(connectionString))
            {
                return await context.Accounts.ToListAsync().ConfigureAwait(false);
            }
        }
    }
}
