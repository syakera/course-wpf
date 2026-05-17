using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class DoctorManagementViewModel : INotifyPropertyChanged
    {
        private readonly DoctorManagementService _doctorService = new DoctorManagementService();
        private readonly MedicalServiceService _medicalServiceService = new MedicalServiceService();

        public ObservableCollection<Doctor> Doctors { get; } = new ObservableCollection<Doctor>();
        public ObservableCollection<ServiceCheckItem> AvailableServices { get; } = new ObservableCollection<ServiceCheckItem>();

        private Doctor _selectedDoctor;
        public Doctor SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged(nameof(SelectedDoctor));
                OnPropertyChanged(nameof(SelectedDoctorFullName));
                OnPropertyChanged(nameof(SelectedDoctorSpecialization));
                OnPropertyChanged(nameof(SelectedDoctorExperience));
                OnPropertyChanged(nameof(SelectedDoctorIsActive));
                RefreshServiceCheckmarks();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedDoctorFullName
        {
            get => _selectedDoctor?.FullName;
            set { if (_selectedDoctor != null) { _selectedDoctor.FullName = value; OnPropertyChanged(nameof(SelectedDoctorFullName)); } }
        }

        public string SelectedDoctorSpecialization
        {
            get => _selectedDoctor?.Specialization;
            set { if (_selectedDoctor != null) { _selectedDoctor.Specialization = value; OnPropertyChanged(nameof(SelectedDoctorSpecialization)); } }
        }

        public int SelectedDoctorExperience
        {
            get => _selectedDoctor?.ExperienceYears ?? 0;
            set { if (_selectedDoctor != null) { _selectedDoctor.ExperienceYears = value; OnPropertyChanged(nameof(SelectedDoctorExperience)); } }
        }

        public bool SelectedDoctorIsActive
        {
            get => _selectedDoctor?.IsActive ?? false;
            set { if (_selectedDoctor != null) { _selectedDoctor.IsActive = value; OnPropertyChanged(nameof(SelectedDoctorIsActive)); } }
        }

        public ICommand RefreshCommand { get; }
        public ICommand NewDoctorCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeactivateCommand { get; }

        public DoctorManagementViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadDoctors());
            NewDoctorCommand = new RelayCommand(_ => CreateNewDoctor());
            SaveCommand = new RelayCommand(_ => SaveDoctor(), _ => SelectedDoctor != null);
            CancelCommand = new RelayCommand(_ => LoadDoctors());
            DeactivateCommand = new RelayCommand(_ => DeactivateDoctor(), _ => SelectedDoctor != null && SelectedDoctor.Id > 0);

            LoadAvailableServices();
            LoadDoctors();
        }

        private void LoadDoctors()
        {
            try
            {
                var doctors = _doctorService.GetAllDoctors();
                Doctors.Clear();
                foreach (var d in doctors) Doctors.Add(d);
                SelectedDoctor = Doctors.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить врачей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableServices()
        {
            try
            {
                AvailableServices.Clear();
                foreach (var svc in _medicalServiceService.LoadServices())
                {
                    AvailableServices.Add(new ServiceCheckItem
                    {
                        Service = svc,
                        Title = svc.ShortName,
                        IsChecked = false
                    });
                }
            }
            catch { }
        }

        private void RefreshServiceCheckmarks()
        {
            var ids = new HashSet<int>(SelectedDoctor?.Services?.Select(s => s.Id) ?? Enumerable.Empty<int>());
            foreach (var item in AvailableServices)
                item.IsChecked = ids.Contains(item.Service.Id);
        }

        private void CreateNewDoctor()
        {
            SelectedDoctor = new Doctor
            {
                FullName = "Новый врач",
                Specialization = "",
                ExperienceYears = 0,
                IsActive = true
            };
            Doctors.Add(SelectedDoctor);
            RefreshServiceCheckmarks();
        }

        private void SaveDoctor()
        {
            try
            {
                if (SelectedDoctor == null) return;
                SelectedDoctor.Services = AvailableServices
                    .Where(x => x.IsChecked)
                    .Select(x => x.Service)
                    .ToList();
                bool isNew = SelectedDoctor.Id == 0;
                int id = _doctorService.SaveDoctor(SelectedDoctor);
                SelectedDoctor.Id = id;
                if (isNew)
                {
                    MessageBox.Show($"Врач добавлен.\nЛогин: {BuildLoginFromFullName(SelectedDoctor.FullName)}\nПароль: doctor",
                        "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                LoadDoctors();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить врача: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeactivateDoctor()
        {
            try
            {
                if (SelectedDoctor == null) return;
                _doctorService.DeleteDoctor(SelectedDoctor.Id);
                LoadDoctors();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось деактивировать врача: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string BuildLoginFromFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "doctor";
            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "doctor" : parts[0];
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ServiceCheckItem : INotifyPropertyChanged
    {
        private bool _isChecked;

        public MedicalService Service { get; set; }
        public string Title { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
