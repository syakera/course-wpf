using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public class MedicalServiceRepository : Repository<MedicalServiceEntity>, IMedicalServiceRepository
    {
        public MedicalServiceRepository(MedicalCenterDbContext context) : base(context)
        {
        }

        public IQueryable<MedicalServiceEntity> QueryWithDetails()
        {
            return Set
                .Include(x => x.Department)
                .Include(x => x.ServiceTimeSlots);
        }

        public Task<MedicalServiceEntity> GetByIdWithSlotsAsync(int id)
        {
            return Set
                .Include(x => x.ServiceTimeSlots)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
