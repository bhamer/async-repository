using System;
using System.Threading.Tasks;

namespace AsyncRepository.UnitsOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Sends all changes to the data store and commits as a single transaction unless a manual transaction was created using BeginTransaction().
        /// </summary>
        void Commit(string changeUser);
        
        /// <summary>
        /// Asynchronously sends all changes to the data store and commits as a single transaction unless a manual transaction was created using BeginTransaction().
        /// </summary>
        Task CommitAsync(string changeUser);

        /// <summary>
        /// Starts a transaction. This can be used to call Commit() multiple times and treat it as a single transaction.
        /// </summary>
        IDisposable BeginTransaction();

        /// <summary>
        /// Commits the transaction that was started using BeginTransaction().
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Rolls back the transaction that was started using BeginTransaction().
        /// </summary>
        void RollbackTransaction();
    }
}
