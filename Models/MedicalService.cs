using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MedicalCenter.Models
{
    public class MedicalService : INotifyPropertyChanged
    {
        private int _id;
        private string _shortName;
        private string _fullName;
        private string _description;
        private string _category;
        private double _rating;
        private decimal _price;
        private int _duration;
        private string _specialist;
        private string _specialistImage;
        private string _department;
        private bool _isPopular;
        private bool _hasDiscount;
        private double _discountPercent;
        private int _patientsCount;
        private List<string> _images;
        private List<TimeSlot> _timeSlots;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string ShortName
        {
            get => _shortName;
            set { _shortName = value; OnPropertyChanged(nameof(ShortName)); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public List<string> Images
        {
            get => _images ?? (_images = new List<string>());
            set { _images = value; OnPropertyChanged(nameof(Images)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public double Rating
        {
            get => _rating;
            set { _rating = value; OnPropertyChanged(nameof(Rating)); }
        }

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); OnPropertyChanged(nameof(FinalPrice)); }
        }

        public int Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); }
        }

        public string Specialist
        {
            get => _specialist;
            set { _specialist = value; OnPropertyChanged(nameof(Specialist)); }
        }

        public string SpecialistImage
        {
            get => _specialistImage;
            set { _specialistImage = value; OnPropertyChanged(nameof(SpecialistImage)); }
        }

        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(nameof(Department)); }
        }

        public bool IsPopular
        {
            get => _isPopular;
            set { _isPopular = value; OnPropertyChanged(nameof(IsPopular)); }
        }

        public bool HasDiscount
        {
            get => _hasDiscount;
            set { _hasDiscount = value; OnPropertyChanged(nameof(HasDiscount)); OnPropertyChanged(nameof(FinalPrice)); }
        }

        public double DiscountPercent
        {
            get => _discountPercent;
            set { _discountPercent = value; OnPropertyChanged(nameof(DiscountPercent)); OnPropertyChanged(nameof(FinalPrice)); }
        }

        public int PatientsCount
        {
            get => _patientsCount;
            set { _patientsCount = value; OnPropertyChanged(nameof(PatientsCount)); }
        }

        public List<TimeSlot> TimeSlots
        {
            get => _timeSlots ?? (_timeSlots = new List<TimeSlot>());
            set { _timeSlots = value; OnPropertyChanged(nameof(TimeSlots)); }
        }

        public decimal FinalPrice => HasDiscount && DiscountPercent > 0
            ? Price * (1 - (decimal)(DiscountPercent / 100))
            : Price;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TimeSlot : INotifyPropertyChanged
    {
        private string _time;
        private bool _isAvailable;
        private string _doctorName;

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(nameof(Time)); }
        }

        public bool IsAvailable
        {
            get => _isAvailable;
            set { _isAvailable = value; OnPropertyChanged(nameof(IsAvailable)); }
        }

        public string DoctorName
        {
            get => _doctorName;
            set { _doctorName = value; OnPropertyChanged(nameof(DoctorName)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Time { get; set; }
        public string Doctor { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
        public int? PatientRating { get; set; }

        public string Date => AppointmentDate.ToString("dd.MM.yyyy");

        public bool IsActive => Status == "Ожидает" || Status == "Подтверждена";

        public bool CanBeRated => Status == "Завершена";
    }
}