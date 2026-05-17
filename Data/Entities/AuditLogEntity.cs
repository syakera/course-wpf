using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("AuditLog")]
    public sealed class AuditLogEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityName { get; set; }

        public int? EntityId { get; set; }

        [Required]
        [StringLength(20)]
        public string ActionType { get; set; }

        public DateTime ChangedAt { get; set; }
    }
}
