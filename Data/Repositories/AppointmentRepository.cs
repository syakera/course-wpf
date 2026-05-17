using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public class AppointmentRepository : Repository<AppointmentEntity>, IAppointmentRepository
    {
        public AppointmentRepository(MedicalCenterDbContext context) : base(context)
        {
        }

        public IQueryable<AppointmentEntity> QueryWithService()
        {
            return Set.Include(x => x.Service);
        }

        public Task<AppointmentEntity> GetByIdAsync(int id)
        {
            return Set.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
