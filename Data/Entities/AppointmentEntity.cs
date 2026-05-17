using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("Appointments")]
    public sealed class AppointmentEntity
    {
        public AppointmentEntity()
        {
            MedicalNotes = new HashSet<MedicalNoteEntity>();
        }

        [Key]
        public int Id { get; set; }

        public int ServiceId { get; set; }

        [Required]
        [StringLength(120)]
        public string PatientName { get; set; }

        [Required]
        [StringLength(40)]
        public string PatientPhone { get; set; }

        [Column(TypeName = "date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [StringLength(5)]
        public string AppointmentTime { get; set; }

        [Required]
        [StringLength(120)]
        public string DoctorName { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        public int? PatientRating { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public MedicalServiceEntity Service { get; set; }

        public ICollection<MedicalNoteEntity> MedicalNotes { get; set; }
    }
}
