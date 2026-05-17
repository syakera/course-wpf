using System.Collections.Generic;
using System.ComponentModel;

namespace MedicalCenter.Models
{
    public class Doctor : INotifyPropertyChanged
    {
        private int _id;
        private string _fullName;
        private string _specialization;
        private int _experienceYears;
        private string _description;
        private string _photoPath;
        private bool _isActive;
        private int? _userId;
        private List<MedicalService> _services;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); OnPropertyChanged(nameof(ShortName)); }
        }

        public string Specialization
        {
            get => _specialization;
            set { _specialization = value; OnPropertyChanged(nameof(Specialization)); }
        }

        public int ExperienceYears
        {
            get => _experienceYears;
            set { _experienceYears = value; OnPropertyChanged(nameof(ExperienceYears)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string PhotoPath
        {
            get => _photoPath;
            set { _photoPath = value; OnPropertyChanged(nameof(PhotoPath)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public int? UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(nameof(UserId)); }
        }

        public List<MedicalService> Services
        {
            get => _services ?? (_services = new List<MedicalService>());
            set { _services = value; OnPropertyChanged(nameof(Services)); }
        }

        public string ShortName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_fullName)) return string.Empty;
                var parts = _fullName.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                return parts.Length == 0 ? _fullName : parts[0];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
