using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class BookingViewModel : INotifyPropertyChanged
    {
        private readonly MedicalServiceService _medService;
        private readonly DoctorManagementService _doctorService;

        // ── Pre-selected context ───────────────────────────────────────────
        // Flow 1: book via service catalog  → Service is fixed, user picks Doctor
        // Flow 2: book via doctors page     → Doctor is fixed, user picks Service
        public MedicalService PreSelectedService { get; }
        public Doctor PreSelectedDoctor { get; }

        public bool ShowDoctorPicker => PreSelectedService != null && PreSelectedDoctor == null;
        public bool ShowServicePicker => PreSelectedDoctor != null && PreSelectedService == null;

        // The service that will be booked (resolved either directly or through selection)
        private MedicalService _selectedService;
        public MedicalService SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged(nameof(SelectedService));
                OnPropertyChanged(nameof(ServiceDisplay));
                OnPropertyChanged(nameof(DisplayService));
                OnPropertyChanged(nameof(CanConfirm));
                _ = LoadTimeSlotsAsync();
            }
        }

        public string ServiceDisplay =>
            SelectedService?.ShortName ?? PreSelectedService?.ShortName ?? "";

        public MedicalService DisplayService => SelectedService ?? PreSelectedService;

        public string DisplayDoctorName =>
            SelectedDoctor?.FullName ?? PreSelectedDoctor?.FullName ?? "";

        // Available doctors for the selected service (flow 1)
        public ObservableCollection<Doctor> AvailableDoctors { get; } = new ObservableCollection<Doctor>();

        // Available services for the selected doctor (flow 2)
        public ObservableCollection<MedicalService> AvailableServices { get; } = new ObservableCollection<MedicalService>();

        private Doctor _selectedDoctor;
        public Doctor SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged(nameof(SelectedDoctor));
                OnPropertyChanged(nameof(DisplayDoctorName));
                OnPropertyChanged(nameof(CanConfirm));
                _ = LoadTimeSlotsAsync();
            }
        }

        public Window OwnerWindow { get; set; }
        public DateTime TodayDate => DateTime.Today;

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
                _ = LoadTimeSlotsAsync();
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        private string _patientName;
        public string PatientName
        {
            get => _patientName;
            set { _patientName = value; OnPropertyChanged(nameof(PatientName)); OnPropertyChanged(nameof(CanConfirm)); }
        }

        private string _patientPhone;
        public string PatientPhone
        {
            get => _patientPhone;
            set { _patientPhone = value; OnPropertyChanged(nameof(PatientPhone)); OnPropertyChanged(nameof(CanConfirm)); }
        }

        private string _additionalComment;
        public string AdditionalComment
        {
            get => _additionalComment;
            set { _additionalComment = value; OnPropertyChanged(nameof(AdditionalComment)); }
        }

        public ObservableCollection<TimeSlotItemViewModel> AvailableTimeSlots { get; } = new ObservableCollection<TimeSlotItemViewModel>();

        private TimeSlotItemViewModel _selectedTimeSlot;
        public TimeSlotItemViewModel SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set
            {
                _selectedTimeSlot = value;
                OnPropertyChanged(nameof(SelectedTimeSlot));
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        public bool HasAvailableTimes => AvailableTimeSlots.Any(t => t.IsAvailable);

        public bool CanConfirm =>
            !string.IsNullOrWhiteSpace(PatientName)
            && !string.IsNullOrWhiteSpace(PatientPhone)
            && SelectedDate.HasValue
            && SelectedTimeSlot != null
            && ResolveService() != null
            && ResolveDoctor() != null;

        public ICommand SelectTimeCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        // Flow 1: service is pre-selected, pick doctor
        public BookingViewModel(MedicalService service, string patientName = "", string patientPhone = "", Doctor preSelectedDoctor = null)
        {
            _medService = new MedicalServiceService();
            _doctorService = new DoctorManagementService();
            _patientName = patientName;
            _patientPhone = patientPhone;

            if (preSelectedDoctor != null)
            {
                // Flow 2: doctor pre-selected
                PreSelectedDoctor = preSelectedDoctor;
                SelectedDoctor = preSelectedDoctor;
                PreSelectedService = service; // may be null if coming purely from doctor page

                if (service != null)
                {
                    SelectedService = service;
                }
                else
                {
                    // Load services for this doctor
                    LoadServicesForDoctor(preSelectedDoctor);
                }
            }
            else
            {
                // Flow 1: service pre-selected
                PreSelectedService = service ?? throw new ArgumentNullException(nameof(service));
                SelectedService = service;
                // Load doctors for this service
                LoadDoctorsForService(service);
            }

            SelectTimeCommand = new RelayCommand(SelectTime);
            ConfirmCommand = new AsyncRelayCommand(async _ => await ConfirmBookingAsync(), _ => CanConfirm);
            CancelCommand = new RelayCommand(_ => OwnerWindow?.Close());

            SelectedDate = DateTime.Today.AddDays(1);
        }

        private void LoadDoctorsForService(MedicalService service)
        {
            try
            {
                var doctors = _doctorService.GetDoctorsByServiceId(service.Id);
                AvailableDoctors.Clear();
                foreach (var d in doctors)
                    AvailableDoctors.Add(d);
                if (AvailableDoctors.Count == 1)
                    SelectedDoctor = AvailableDoctors[0];
            }
            catch { }
        }

        private void LoadServicesForDoctor(Doctor doctor)
        {
            AvailableServices.Clear();
            if (doctor == null) return;

            var services = doctor.Services;
            if (services == null || services.Count == 0)
            {
                // Defensive fallback: reload doctor with services from DB.
                var freshDoctor = _doctorService.GetById(doctor.Id);
                services = freshDoctor?.Services;
            }

            if (services == null) return;

            foreach (var svc in services)
                AvailableServices.Add(svc);
            if (AvailableServices.Count == 1)
                SelectedService = AvailableServices[0];
        }

        private MedicalService EnsureFullService(MedicalService svc)
        {
            if (svc == null) return null;
            if (svc.TimeSlots != null && svc.TimeSlots.Count > 0) return svc;
            try
            {
                var full = _medService.LoadServices().FirstOrDefault(s => s.Id == svc.Id);
                return full ?? svc;
            }
            catch { return svc; }
        }

        private MedicalService ResolveService() => EnsureFullService(SelectedService ?? PreSelectedService);
        private Doctor ResolveDoctor() => SelectedDoctor ?? PreSelectedDoctor;

        private async Task LoadTimeSlotsAsync()
        {
            AvailableTimeSlots.Clear();
            SelectedTimeSlot = null;

            var service = ResolveService();
            if (!SelectedDate.HasValue || service == null)
            {
                OnPropertyChanged(nameof(HasAvailableTimes));
                return;
            }

            var busyTimes = await _medService.GetBusyTimesAsync(service.Id, SelectedDate.Value.Date).ConfigureAwait(true);
            var busySet = new HashSet<string>(busyTimes, StringComparer.OrdinalIgnoreCase);
            var doctor = ResolveDoctor();
            var doctorName = doctor?.FullName ?? service.Specialist ?? "";
            var day = SelectedDate.Value.Date;
            var now = DateTime.Now;

            var slots = service.TimeSlots;
            if (slots == null || slots.Count == 0)
            {
                // Generate default slots from service if not loaded
                slots = new List<TimeSlot>
                {
                    new TimeSlot { Time = "09:00", IsAvailable = true },
                    new TimeSlot { Time = "10:00", IsAvailable = true },
                    new TimeSlot { Time = "11:00", IsAvailable = true },
                    new TimeSlot { Time = "12:00", IsAvailable = true },
                    new TimeSlot { Time = "13:00", IsAvailable = true },
                    new TimeSlot { Time = "14:00", IsAvailable = true },
                    new TimeSlot { Time = "15:00", IsAvailable = true },
                    new TimeSlot { Time = "16:00", IsAvailable = true },
                    new TimeSlot { Time = "17:00", IsAvailable = true },
                };
            }

            foreach (var slot in slots.Where(t => !string.IsNullOrWhiteSpace(t.Time)))
            {
                var notBusy = slot.IsAvailable && !busySet.Contains(slot.Time);
                var notInPast = AppointmentScheduling.TryGetSlotStart(day, slot.Time, out var slotStart) && slotStart >= now;
                AvailableTimeSlots.Add(new TimeSlotItemViewModel
                {
                    Time = slot.Time,
                    DoctorName = doctorName,
                    IsAvailable = notBusy && notInPast
                });
            }

            OnPropertyChanged(nameof(HasAvailableTimes));
        }

        private void SelectTime(object param)
        {
            if (!(param is TimeSlotItemViewModel slot) || !slot.IsAvailable)
                return;

            foreach (var item in AvailableTimeSlots)
                item.IsSelected = false;

            slot.IsSelected = true;
            SelectedTimeSlot = slot;
        }

        private async Task ConfirmBookingAsync()
        {
            if (!CanConfirm) return;

            var service = ResolveService();
            var doctor = ResolveDoctor();

            var appointment = new Appointment
            {
                ServiceId = service.Id,
                ServiceName = service.ShortName,
                PatientName = PatientName.Trim(),
                PatientPhone = PatientPhone.Trim(),
                AppointmentDate = SelectedDate.Value.Date,
                Time = SelectedTimeSlot.Time,
                Doctor = doctor?.FullName ?? SelectedTimeSlot.DoctorName ?? service.Specialist ?? "",
                Price = service.FinalPrice,
                Comment = string.IsNullOrWhiteSpace(AdditionalComment) ? null : AdditionalComment.Trim(),
                Status = "Ожидает"
            };

            try
            {
                await _medService.CreateAppointmentAsync(appointment).ConfigureAwait(true);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка записи", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadTimeSlotsAsync().ConfigureAwait(true);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения записи: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Запись успешно оформлена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            if (OwnerWindow != null)
            {
                OwnerWindow.DialogResult = true;
                OwnerWindow.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class TimeSlotItemViewModel : TimeSlot, INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
