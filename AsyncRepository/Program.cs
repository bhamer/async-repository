using AsyncRepository.Repositories.Query;
using AsyncRepository.Services;
using AsyncRepository.UnitsOfWork;

namespace AsyncRepository
{
    public class Program
    {
        public void Main()
        {
            var uow = new MyUnitOfWork("connection string");
            var positionQueryRepo = new PositionQueryRepository("connection string");

            // Inject dependencies into services and go have fun
            var positionService = new PositionService(uow, positionQueryRepo);
            var performanceService = new PerformanceService(positionQueryRepo);
        }
    }
}
