using System.Data.Entity;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public class DepartmentRepository : Repository<DepartmentEntity>, IDepartmentRepository
    {
        public DepartmentRepository(MedicalCenterDbContext context) : base(context)
        {
        }

        public Task<DepartmentEntity> GetByNameAsync(string name)
        {
            return Set.FirstOrDefaultAsync(x => x.Name == name);
        }
    }
}
