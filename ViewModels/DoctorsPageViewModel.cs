using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class DoctorsPageViewModel : INotifyPropertyChanged
    {
        private readonly DoctorManagementService _service = new DoctorManagementService();

        public event Action<Doctor, MedicalService> BookingRequested;

        public ObservableCollection<Doctor> Doctors { get; } = new ObservableCollection<Doctor>();
        public ObservableCollection<string> Specializations { get; } = new ObservableCollection<string> { "Все" };

        private string _selectedSpecialization = "Все";
        public string SelectedSpecialization
        {
            get => _selectedSpecialization;
            set { _selectedSpecialization = value; OnPropertyChanged(nameof(SelectedSpecialization)); LoadDoctors(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); LoadDoctors(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand BookDoctorCommand { get; }

        public DoctorsPageViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadDoctors());
            BookDoctorCommand = new RelayCommand(param => HandleBooking(param));
            LoadSpecializations();
            LoadDoctors();
        }

        private void LoadSpecializations()
        {
            try
            {
                var all = _service.GetAllDoctors();
                var specs = all
                    .Select(d => d.Specialization)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                Specializations.Clear();
                Specializations.Add("Все");
                foreach (var s in specs) Specializations.Add(s);
            }
            catch
            {
                // keep default "Все"
            }
        }

        private void LoadDoctors()
        {
            try
            {
                List<Doctor> items = _selectedSpecialization == "Все"
                    ? _service.GetAllDoctors()
                    : _service.GetDoctorsBySpecialization(_selectedSpecialization);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    items = items.Where(d =>
                        (d.FullName ?? "").ToLower().Contains(search) ||
                        (d.Specialization ?? "").ToLower().Contains(search)).ToList();
                }

                Doctors.Clear();
                foreach (var d in items) Doctors.Add(d);
            }
            catch
            {
                Doctors.Clear();
            }
        }

        private void HandleBooking(object parameter)
        {
            if (parameter is Doctor doctor)
            {
                MedicalService service = doctor.Services.Count == 1 ? doctor.Services[0] : null;
                BookingRequested?.Invoke(doctor, service);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
