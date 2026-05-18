using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class AppointmentsViewModel : INotifyPropertyChanged
    {
        private readonly MedicalServiceService _service = new MedicalServiceService();
        private readonly DispatcherTimer _refreshTimer;
        private readonly string _currentUserName;
        private readonly string _currentUserPhone;

        public UserRole CurrentRole { get; }
        public bool IsAdmin => CurrentRole == UserRole.Admin;
        public bool IsDoctor => CurrentRole == UserRole.Doctor;
        public bool IsPatient => CurrentRole == UserRole.Patient;
        public bool IsAppointmentsGridReadOnly => true;

        private ObservableCollection<Appointment> _appointments = new ObservableCollection<Appointment>();
        public ObservableCollection<Appointment> Appointments
        {
            get => _appointments;
            set
            {
                _appointments = value;
                OnPropertyChanged(nameof(Appointments));
                OnPropertyChanged(nameof(HasAppointments));
            }
        }

        public bool HasAppointments => Appointments.Count > 0;

        private Appointment _selectedAppointment;
        public Appointment SelectedAppointment
        {
            get => _selectedAppointment;
            set
            {
                _selectedAppointment = value;
                OnPropertyChanged(nameof(SelectedAppointment));
                OnPropertyChanged(nameof(CanRateSelectedAppointment));
                _ = LoadSelectedAppointmentContextAsync();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _selectedStatus = "Все";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged(nameof(SelectedStatus));
            }
        }

        private string _selectedDoctor = "Все";
        public string SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged(nameof(SelectedDoctor));
            }
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
            }
        }

        private bool _isWeekMode;
        public bool IsWeekMode
        {
            get => _isWeekMode;
            set
            {
                _isWeekMode = value;
                OnPropertyChanged(nameof(IsWeekMode));
            }
        }

        private string _sortOption = "Дата (новые)";
        public string SortOption
        {
            get => _sortOption;
            set
            {
                _sortOption = value;
                OnPropertyChanged(nameof(SortOption));
            }
        }

        private string _noteText;
        public string NoteText
        {
            get => _noteText;
            set
            {
                _noteText = value;
                OnPropertyChanged(nameof(NoteText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _selectedRating = 5;
        public int SelectedRating
        {
            get => _selectedRating;
            set
            {
                _selectedRating = value;
                OnPropertyChanged(nameof(SelectedRating));
                OnPropertyChanged(nameof(CanRateSelectedAppointment));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool CanRateSelectedAppointment =>
            IsPatient &&
            SelectedAppointment != null &&
            SelectedAppointment.CanBeRated &&
            SelectedRating >= 1 &&
            SelectedRating <= 5;

        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string>
        {
            "Все", "Ожидает", "Подтверждена", "Завершена", "Отменена", "Пришел", "Не явился"
        };

        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>
        {
            "Дата (новые)", "Дата (старые)", "Цена (возр)", "Цена (уб)"
        };

        public ObservableCollection<string> Doctors { get; } = new ObservableCollection<string> { "Все" };
        public ObservableCollection<int> RatingOptions { get; } = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
        public ObservableCollection<Appointment> PatientHistory { get; } = new ObservableCollection<Appointment>();
        public ObservableCollection<MedicalNote> AppointmentNotes { get; } = new ObservableCollection<MedicalNote>();

        public ICommand RefreshCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand SetStatusCommand { get; }
        public ICommand DeleteAppointmentCommand { get; }
        public ICommand CancelAppointmentCommand { get; }
        public ICommand AddRecipeCommand { get; }
        public ICommand AddReferralCommand { get; }
        public ICommand RateAppointmentCommand { get; }

        public AppointmentsViewModel(UserRole role, string currentUserName, string currentUserPhone)
        {
            CurrentRole = role;
            _currentUserName = currentUserName ?? "";
            _currentUserPhone = currentUserPhone ?? "";

            RefreshCommand = new AsyncRelayCommand(async _ => await ApplyFilterAsync());
            ApplyFilterCommand = new AsyncRelayCommand(async _ => await ApplyFilterAsync());
            SetStatusCommand = new AsyncRelayCommand(async status => await ChangeStatusAsync(status as string),
                _ => SelectedAppointment != null
                     && (IsAdmin || (IsDoctor && SelectedAppointment.Status != "Отменена")));
            DeleteAppointmentCommand = new AsyncRelayCommand(async _ => await DeleteSelectedAsync(), _ => IsAdmin && SelectedAppointment != null);
            CancelAppointmentCommand = new AsyncRelayCommand(async _ => await CancelSelectedAsync(), _ => IsPatient && SelectedAppointment != null && SelectedAppointment.IsActive);
            AddRecipeCommand = new AsyncRelayCommand(async _ => await AddNoteAsync("Рецепт"),
                _ => IsDoctor && SelectedAppointment != null && SelectedAppointment.Status != "Отменена" && !string.IsNullOrWhiteSpace(NoteText));
            AddReferralCommand = new AsyncRelayCommand(async _ => await AddNoteAsync("Направление"),
                _ => IsDoctor && SelectedAppointment != null && SelectedAppointment.Status != "Отменена" && !string.IsNullOrWhiteSpace(NoteText));
            RateAppointmentCommand = new AsyncRelayCommand(async _ => await RateSelectedAppointmentAsync(), _ => CanRateSelectedAppointment);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _refreshTimer.Tick += async (_, __) =>
            {
                try
                {
                    await ApplyFilterAsync().ConfigureAwait(true);
                }
                catch
                {
                    // Ignore timer refresh failures to keep UI alive.
                }
            };
            _refreshTimer.Start();

            _ = InitializeAsync();
        }

        public void StopAutoRefresh()
        {
            _refreshTimer.Stop();
        }

        private async Task InitializeAsync()
        {
            var doctors = await _service.GetDoctorsAsync().ConfigureAwait(true);
            foreach (var doctor in doctors)
                Doctors.Add(doctor);

            if (IsDoctor && doctors.Count > 0)
                SelectedDoctor = doctors.Contains(_currentUserName) ? _currentUserName : doctors[0];

            await ApplyFilterAsync().ConfigureAwait(true);
        }

        private async Task ApplyFilterAsync()
        {
            var items = await GetByRoleAsync().ConfigureAwait(true);

            if (SelectedStatus != "Все")
                items = items.Where(a => a.Status == SelectedStatus).ToList();

            if (SelectedDoctor != "Все")
                items = items.Where(a => a.Doctor == SelectedDoctor).ToList();

            if (IsDoctor || IsWeekMode)
            {
                var startDate = IsWeekMode ? StartOfWeek(SelectedDate) : SelectedDate.Date;
                var endDate = IsWeekMode ? startDate.AddDays(6) : SelectedDate.Date;
                items = items.Where(a => a.AppointmentDate.Date >= startDate && a.AppointmentDate.Date <= endDate).ToList();
            }

            Appointments = new ObservableCollection<Appointment>(ApplySorting(items));
        }

        private async Task<List<Appointment>> GetByRoleAsync()
        {
            if (IsAdmin)
                return await _service.GetAppointmentsAsync().ConfigureAwait(true);

            if (IsDoctor)
            {
                var startDate = IsWeekMode ? StartOfWeek(SelectedDate) : SelectedDate.Date;
                var endDate = IsWeekMode ? startDate.AddDays(6) : SelectedDate.Date;
                return await _service.GetAppointmentsForDoctorAsync(SelectedDoctor == "Все" ? _currentUserName : SelectedDoctor, startDate, endDate).ConfigureAwait(true);
            }

            return await _service.GetAppointmentsForPatientAsync(string.IsNullOrWhiteSpace(_currentUserPhone) ? _currentUserName : _currentUserPhone).ConfigureAwait(true);
        }

        private async Task ChangeStatusAsync(string status)
        {
            if (SelectedAppointment == null || string.IsNullOrWhiteSpace(status))
                return;

            try
            {
                var changedByRole = IsAdmin ? UserRole.Admin : UserRole.Doctor;
                await _service.UpdateAppointmentStatusAsync(SelectedAppointment.Id, status, changedByRole).ConfigureAwait(true);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Действие недоступно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ApplyFilterAsync().ConfigureAwait(true);
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedAppointment == null)
                return;

            var confirm = MessageBox.Show($"Удалить запись пациента {SelectedAppointment.PatientName}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            await _service.DeleteAppointmentAsync(SelectedAppointment.Id).ConfigureAwait(true);
            await ApplyFilterAsync().ConfigureAwait(true);
        }

        private async Task CancelSelectedAsync()
        {
            if (SelectedAppointment == null)
                return;

            await _service.UpdateAppointmentStatusAsync(SelectedAppointment.Id, "Отменена").ConfigureAwait(true);
            await ApplyFilterAsync().ConfigureAwait(true);
        }

        private async Task AddNoteAsync(string noteType)
        {
            if (SelectedAppointment == null || string.IsNullOrWhiteSpace(NoteText))
                return;

            await _service.AddMedicalNoteAsync(SelectedAppointment.Id, noteType, NoteText.Trim(), SelectedAppointment.Doctor).ConfigureAwait(true);
            NoteText = "";
            await LoadSelectedAppointmentContextAsync().ConfigureAwait(true);
        }

        private async Task LoadSelectedAppointmentContextAsync()
        {
            PatientHistory.Clear();
            AppointmentNotes.Clear();

            if (SelectedAppointment == null)
                return;

            var history = await _service.GetPatientHistoryAsync(SelectedAppointment.PatientPhone).ConfigureAwait(true);
            foreach (var item in history.Where(h => h.Id != SelectedAppointment.Id).Take(10))
                PatientHistory.Add(item);

            var notes = await _service.GetMedicalNotesByAppointmentAsync(SelectedAppointment.Id).ConfigureAwait(true);
            foreach (var note in notes)
                AppointmentNotes.Add(note);

            SelectedRating = SelectedAppointment.PatientRating ?? 5;
            OnPropertyChanged(nameof(CanRateSelectedAppointment));
        }

        private async Task RateSelectedAppointmentAsync()
        {
            if (!CanRateSelectedAppointment)
                return;

            await _service.UpdateAppointmentRatingAsync(SelectedAppointment.Id, SelectedRating).ConfigureAwait(true);
            await ApplyFilterAsync().ConfigureAwait(true);
            MessageBox.Show("Оценка сохранена.", "Спасибо", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private IEnumerable<Appointment> ApplySorting(IEnumerable<Appointment> items)
        {
            switch (SortOption)
            {
                case "Дата (старые)":
                    return items.OrderBy(a => a.AppointmentDate).ThenBy(a => a.Time);
                case "Цена (возр)":
                    return items.OrderBy(a => a.Price);
                case "Цена (уб)":
                    return items.OrderByDescending(a => a.Price);
                default:
                    return items.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.Time);
            }
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
