using AsyncRepository.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncRepository.Repositories.Query
{
    public interface ITradeQueryRepository
    {
        Task<IEnumerable<Trade>> GetTradesForAccountAsync(string accountCode, DateTime tradeDate);
    }
}
