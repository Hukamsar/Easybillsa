using EasyBill.Models.ViewModels;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IHomeIndexDashboardDataRepository
    {
        Task<HomeIndexCoreDatasets> LoadCoreDatasetsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
