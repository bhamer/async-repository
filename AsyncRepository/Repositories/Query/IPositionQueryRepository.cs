using AsyncRepository.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncRepository.Repositories.Query
{
    public interface IPositionQueryRepository
    {
        Task<IEnumerable<Position>> GetPositionsForAccountAsync(string accountCode, DateTime positionDate);
    }
}
