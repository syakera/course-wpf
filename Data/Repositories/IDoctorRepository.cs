using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public interface IDoctorRepository : IRepository<DoctorEntity>
    {
        IQueryable<DoctorEntity> QueryWithServices();
        Task<DoctorEntity> GetByIdAsync(int id);
        Task<List<DoctorEntity>> GetActiveAsync();
        Task<DoctorEntity> GetByFullNameAsync(string fullName);
    }
}
