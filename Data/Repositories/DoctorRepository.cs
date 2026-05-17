using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public class DoctorRepository : Repository<DoctorEntity>, IDoctorRepository
    {
        public DoctorRepository(MedicalCenterDbContext context) : base(context)
        {
        }

        public IQueryable<DoctorEntity> QueryWithServices()
        {
            return Set
                .Include(d => d.User)
                .Include(d => d.DoctorServices.Select(ds => ds.Service));
        }

        public Task<DoctorEntity> GetByIdAsync(int id)
        {
            return Set
                .Include(d => d.User)
                .Include(d => d.DoctorServices.Select(ds => ds.Service))
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public Task<List<DoctorEntity>> GetActiveAsync()
        {
            return Set
                .Include(d => d.User)
                .Include(d => d.DoctorServices.Select(ds => ds.Service))
                .Where(d => d.IsActive)
                .OrderBy(d => d.FullName)
                .ToListAsync();
        }

        public Task<DoctorEntity> GetByFullNameAsync(string fullName)
        {
            return Set.FirstOrDefaultAsync(d => d.FullName == fullName);
        }
    }
}
