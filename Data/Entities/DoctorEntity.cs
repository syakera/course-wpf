using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("Doctors")]
    public sealed class DoctorEntity
    {
        public DoctorEntity()
        {
            DoctorServices = new HashSet<DoctorServiceEntity>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; }

        [StringLength(150)]
        public string Specialization { get; set; }

        public int ExperienceYears { get; set; }

        public string Description { get; set; }

        [StringLength(500)]
        public string PhotoPath { get; set; }

        public bool IsActive { get; set; }

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserEntity User { get; set; }

        public ICollection<DoctorServiceEntity> DoctorServices { get; set; }
    }
}
