using System.ComponentModel;

namespace MedicalCenter.Models
{
    public class UserProfile : INotifyPropertyChanged
    {
        private int _id;
        private string _username;
        private UserRole _role;
        private string _fullName;
        private string _phone;
        private string _email;
        private string _avatarPath;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username == value) return;
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public UserRole Role
        {
            get => _role;
            set
            {
                if (_role == value) return;
                _role = value;
                OnPropertyChanged(nameof(Role));
            }
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName == value) return;
                _fullName = value;
                OnPropertyChanged(nameof(FullName));
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone == value) return;
                _phone = value;
                OnPropertyChanged(nameof(Phone));
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email == value) return;
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string AvatarPath
        {
            get => _avatarPath;
            set
            {
                if (_avatarPath == value) return;
                _avatarPath = value;
                OnPropertyChanged(nameof(AvatarPath));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
