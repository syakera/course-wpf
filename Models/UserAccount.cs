namespace MedicalCenter.Models
{
    public enum UserRole
    {
        Admin = 0,
        Doctor = 1,
        Patient = 2
    }

    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public UserRole Role { get; set; }
        public string DisplayName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AvatarPath { get; set; }
    }
}
