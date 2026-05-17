using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public interface IMedicalServiceRepository : IRepository<MedicalServiceEntity>
    {
        IQueryable<MedicalServiceEntity> QueryWithDetails();
        Task<MedicalServiceEntity> GetByIdWithSlotsAsync(int id);
    }
}
