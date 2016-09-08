using AsyncRepository.Models;
using AsyncRepository.Repositories.Command;

namespace AsyncRepository.UnitsOfWork
{
    // ORM-agnostic unit of work
    public interface IMyUnitOfWork : IUnitOfWork
    {
        ICommandRepository<Account> AccountRepository { get; }
        ICommandRepository<Position> PositionRepository { get; }
        ICommandRepository<Trade> TradeRepository { get; }
    }
}
