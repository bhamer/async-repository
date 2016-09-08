using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncRepository.Models;
using System.Data.SqlClient;
using Dapper;

namespace AsyncRepository.Repositories.Query
{
    public class TradeQueryRepository : ITradeQueryRepository
    {
        private readonly string connectionString;

        public TradeQueryRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<Trade>> GetTradesForAccountAsync(string accountCode, DateTime tradeDate)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<Trade>("select * from Trade where AccountCode = @AccountCode and TradeDate = @TradeDate", 
                    new { AccountCode = accountCode, TradeDate = tradeDate }).ConfigureAwait(false);
            }
        }
    }
}
