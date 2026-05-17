using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public class MedicalNoteRepository : Repository<MedicalNoteEntity>, IMedicalNoteRepository
    {
        public MedicalNoteRepository(MedicalCenterDbContext context) : base(context)
        {
        }

        public Task<List<MedicalNoteEntity>> GetByAppointmentAsync(int appointmentId)
        {
            return Set
                .Where(n => n.AppointmentId == appointmentId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}
