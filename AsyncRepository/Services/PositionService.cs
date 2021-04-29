using AsyncRepository.Models;
using AsyncRepository.Repositories.Query;
using AsyncRepository.UnitsOfWork;
using System;

namespace AsyncRepository.Services
{
    public class PositionService
    {
        private readonly IMyUnitOfWork myUnitOfWork;
        IPositionQueryRepository positionQueryRepo;

        public PositionService(IMyUnitOfWork myUnitOfWork, IPositionQueryRepository positionQueryRepo)
        {
            this.myUnitOfWork = myUnitOfWork;
            this.positionQueryRepo = positionQueryRepo;
        }

        public void AddOrUpdatePosition(Trade trade)
        {
            if (trade == null) throw new ArgumentNullException(nameof(trade));

            // verify the account exists
            if (myUnitOfWork.AccountRepository.Find(trade.AccountCode) == null) throw new ArgumentException("bad account code");

            // try to get existing position (note: I'm using .Result here because I'm fine with this method being synchronous)
            var position = positionQueryRepo.GetPositionAsync(trade.AccountCode, trade.SecurityId).Result;

            // if a position doesn't already exist, create it; otherwise, update it
            if (position == null)
            {
                CreatePosition(trade);
            }
            else
            {
                UpdatePosition(position, trade);
            }

            // save changes to data store in a single transaction
            myUnitOfWork.Commit("username");
        }

        private void CreatePosition(Trade trade)
        {
            // add trade
            myUnitOfWork.TradeRepository.Add(trade);

            // create and add position
            var position = new Position() { 
                AccountCode = trade.AccountCode, 
                PositionDate = trade.TradeDate, 
                MarketValue = trade.MarketValue,
                SecurityId = trade.SecurityId
            };
            myUnitOfWork.PositionRepository.Add(position);
        }

        private void UpdatePosition(Position position, Trade trade)
        {
            // update position value
            position.MarketValue += trade.MarketValue;

            // tell UOW (EF) that this position has been modified
            myUnitOfWork.PositionRepository.Update(position);
        }
    }
}
