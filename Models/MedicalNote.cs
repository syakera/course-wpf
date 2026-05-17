using System;

namespace MedicalCenter.Models
{
    public class MedicalNote
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string NoteType { get; set; }
        public string Content { get; set; }
        public string DoctorName { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CreatedAtDisplay => CreatedAt.ToString("dd.MM.yyyy HH:mm");
    }

    public static class MedicalNoteLimits
    {
        public const int MaxContentLength = 2000;
    }
}
