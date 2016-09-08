using AsyncRepository.Models;
using AsyncRepository.UnitsOfWork;
using System;

namespace AsyncRepository.Services
{
    public class PositionService
    {
        private readonly IMyUnitOfWork myUnitOfWork;

        public PositionService(IMyUnitOfWork myUnitOfWork)
        {
            this.myUnitOfWork = myUnitOfWork;        
        }
        
        public void OpenPosition(string accountCode, Trade trade)
        {
            // verify the account exists
            if (myUnitOfWork.AccountRepository.Find(accountCode) == null) throw new ArgumentException("bad account code");

            // add trade
            myUnitOfWork.TradeRepository.Add(trade);

            // create and add position
            var position = new Position() { AccountCode = accountCode, PositionDate = trade.TradeDate, MarketValue = trade.MarketValue };
            myUnitOfWork.PositionRepository.Add(position);

            // save changes to data store in a single transaction
            myUnitOfWork.Commit();
        }
    }
}
