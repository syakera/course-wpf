using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("Departments")]
    public sealed class DepartmentEntity
    {
        public DepartmentEntity()
        {
            MedicalServices = new HashSet<MedicalServiceEntity>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public ICollection<MedicalServiceEntity> MedicalServices { get; set; }
    }
}
