using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public interface IAppointmentRepository : IRepository<AppointmentEntity>
    {
        IQueryable<AppointmentEntity> QueryWithService();
        Task<AppointmentEntity> GetByIdAsync(int id);
    }
}
