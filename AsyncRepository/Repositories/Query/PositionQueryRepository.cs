using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncRepository.Models;
using AsyncRepository.UnitsOfWork;
using System.Data.SqlClient;
using Dapper;
using System.Data.Entity;

namespace AsyncRepository.Repositories.Query
{
    public class PositionQueryRepository : IPositionQueryRepository
    {
        private readonly string connectionString;
        
        public PositionQueryRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // using Dapper.NET
        public async Task<IEnumerable<Position>> GetPositionsForAccountAsync(string accountCode, DateTime positionDate)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<Position>("select * from Position where AccountCode = @AccountCode and PositionDate = @PositionDate", 
                    new { AccountCode = accountCode, PositionDate = positionDate }).ConfigureAwait(false);
            }
        }

        // using EF6
        public async Task<List<Position>> EF_GetPositionsForAccountAsync(string accountCode, DateTime positionDate)
        {
            using (var context = new MyDbContext(connectionString))
            {
                return await context.Positions.Where(p => p.AccountCode == accountCode && p.PositionDate == positionDate)
                    .ToListAsync().ConfigureAwait(false);
            }
        }

        public async Task<Position> GetPositionAsync(string accountCode, int securityId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryFirstOrDefaultAsync<Position>("select * from Position where AccountCode = @AccountCode and SecurityId = @SecurityId",
                    new { AccountCode = accountCode, SecurityId = securityId }).ConfigureAwait(false);
            }
        }
    }
}
