using System;
using System.Threading.Tasks;
using AsyncRepository.Models;
using AsyncRepository.Repositories.Command;

namespace AsyncRepository.UnitsOfWork
{
    // Implemenation of IMyUnitOfWork using EF DbContext
    public class MyUnitOfWork : MyDbContext, IMyUnitOfWork
    {
        public MyUnitOfWork(string connectionString)
            : base(connectionString)
        {
            accountRepo = new Lazy<ICommandRepository<Account>>(() => new EFCommandRepository<Account>(this));
            positionRepo = new Lazy<ICommandRepository<Position>>(() => new EFCommandRepository<Position>(this));
            tradeRepo = new Lazy<ICommandRepository<Trade>>(() => new EFCommandRepository<Trade>(this));
        }

        private readonly Lazy<ICommandRepository<Account>> accountRepo;
        public ICommandRepository<Account> AccountRepository
        {
            get { return accountRepo.Value; }
        }

        private readonly Lazy<ICommandRepository<Position>> positionRepo;
        public ICommandRepository<Position> PositionRepository
        {
            get { return positionRepo.Value; }
        }

        private readonly Lazy<ICommandRepository<Trade>> tradeRepo;
        public ICommandRepository<Trade> TradeRepository
        {
            get { return tradeRepo.Value; }
        }

        public void Commit()
        {
            SaveChanges();
        }

        public Task CommitAsync()
        {
            return SaveChangesAsync();
        }
    }
}
