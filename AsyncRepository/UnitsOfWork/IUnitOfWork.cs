using System;
using System.Threading.Tasks;

namespace AsyncRepository.UnitsOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        void Commit();
        Task CommitAsync();
    }
}
