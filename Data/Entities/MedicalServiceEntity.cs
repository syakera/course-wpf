using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("MedicalServices")]
    public sealed class MedicalServiceEntity
    {
        public MedicalServiceEntity()
        {
            ServiceTimeSlots = new HashSet<ServiceTimeSlotEntity>();
            Appointments = new HashSet<AppointmentEntity>();
            DoctorServices = new HashSet<DoctorServiceEntity>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ShortName { get; set; }

        [StringLength(200)]
        public string FullName { get; set; }

        public string Description { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        public double Rating { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Price { get; set; }

        public int Duration { get; set; }

        [StringLength(150)]
        public string Specialist { get; set; }

        public byte[] SpecialistImage { get; set; }

        public int DepartmentId { get; set; }

        public bool IsPopular { get; set; }
        public bool HasDiscount { get; set; }
        public double DiscountPercent { get; set; }
        public int PatientsCount { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public DepartmentEntity Department { get; set; }

        public ICollection<ServiceTimeSlotEntity> ServiceTimeSlots { get; set; }
        public ICollection<AppointmentEntity> Appointments { get; set; }
        public ICollection<DoctorServiceEntity> DoctorServices { get; set; }
    }
}
