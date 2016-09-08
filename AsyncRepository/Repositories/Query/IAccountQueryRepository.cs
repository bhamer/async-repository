using AsyncRepository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncRepository.Repositories.Query
{
    public interface IAccountQueryRepository
    {
        Task<List<Account>> GetAccounts(); // using List instead of IEnumerable because Task<T> doesn't support covariance
    }
}
