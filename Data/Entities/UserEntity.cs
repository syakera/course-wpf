using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalCenter.Data.Entities
{
    [Table("Users")]
    public sealed class UserEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Username { get; set; }

        [Required]
        [StringLength(200)]
        public string Password { get; set; }

        // 0 = Admin, 1 = Doctor, 2 = Patient
        public int Role { get; set; }

        [StringLength(150)]
        public string DisplayName { get; set; }

        [StringLength(40)]
        public string Phone { get; set; }

        [StringLength(150)]
        public string Email { get; set; }

        [StringLength(500)]
        public string AvatarPath { get; set; }
    }
}
