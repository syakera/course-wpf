using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("MedicalNotes")]
    public sealed class MedicalNoteEntity
    {
        [Key]
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string NoteType { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(150)]
        public string DoctorName { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public AppointmentEntity Appointment { get; set; }
    }
}
