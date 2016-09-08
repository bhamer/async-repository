using AsyncRepository.Repositories.Query;
using AsyncRepository.Services;
using AsyncRepository.UnitsOfWork;

namespace AsyncRepository
{
    public class Program
    {
        public void Main()
        {
            // Inject dependencies into services and go have fun            
            var positionsService = new PositionService(new MyUnitOfWork("connection string"));
            var performanceService = new PerformanceService(new PositionQueryRepository("connection string"));
        }
    }
}
