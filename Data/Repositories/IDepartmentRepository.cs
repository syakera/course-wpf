using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public interface IDepartmentRepository : IRepository<DepartmentEntity>
    {
        Task<DepartmentEntity> GetByNameAsync(string name);
    }
}
