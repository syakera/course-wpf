using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("ServiceTimeSlots")]
    public sealed class ServiceTimeSlotEntity
    {
        [Key]
        public int Id { get; set; }

        public int ServiceId { get; set; }

        [Required]
        [StringLength(5)]
        public string SlotTime { get; set; }

        [Column("IsDefaultAvailable")]
        public bool IsAvailable { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public MedicalServiceEntity MedicalService { get; set; }
    }
}
