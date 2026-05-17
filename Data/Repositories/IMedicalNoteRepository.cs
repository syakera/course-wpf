using System.Collections.Generic;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public interface IMedicalNoteRepository : IRepository<MedicalNoteEntity>
    {
        Task<List<MedicalNoteEntity>> GetByAppointmentAsync(int appointmentId);
    }
}
