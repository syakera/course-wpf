using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("DoctorServices")]
    public sealed class DoctorServiceEntity
    {
        public int DoctorId { get; set; }

        public int ServiceId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public DoctorEntity Doctor { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public MedicalServiceEntity Service { get; set; }
    }
}
