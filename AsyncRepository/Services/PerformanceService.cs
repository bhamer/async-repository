using AsyncRepository.Repositories.Query;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncRepository.Services
{
    public class PerformanceService
    {
        IPositionQueryRepository positionQueryRepo;

        public PerformanceService(IPositionQueryRepository positionQueryRepo)
        {
            this.positionQueryRepo = positionQueryRepo;
        }
        
        public async Task<decimal> CalculateDailyGainLoss(string accountCode, DateTime positionDate)
        {
            // get T and T-1 positions
            var todaysPositionsTask = positionQueryRepo.GetPositionsForAccountAsync(accountCode, positionDate.Date);            
            var yesterdaysPositionsTask = positionQueryRepo.GetPositionsForAccountAsync(accountCode, positionDate.AddDays(-1).Date);

            // wait for both methods to complete asynchronously
            await Task.WhenAll(todaysPositionsTask, yesterdaysPositionsTask).ConfigureAwait(false);

            // calculate day over day GL using today's and yesterday's positions
            return todaysPositionsTask.Result.Sum(p => p.MarketValue) - yesterdaysPositionsTask.Result.Sum(p => p.MarketValue);            
        }
    }
}
