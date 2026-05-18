using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;
using MedicalCenter.Services.Notifications;
using MedicalCenter.Views;

namespace MedicalCenter.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IObserver<AppointmentStatusChangedEvent>
    {
        private const string AllCategoriesKey = "__all__";
        private const int MaxUndoSteps = 30;

        public sealed class CategoryOption : INotifyPropertyChanged
        {
            public string Key { get; }

            private string _display;
            public string Display
            {
                get => _display;
                set
                {
                    if (_display == value) return;
                    _display = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
                }
            }

            public CategoryOption(string key, string display)
            {
                Key = key;
                _display = display;
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private readonly MedicalServiceService _service = new MedicalServiceService();
        private List<MedicalService> _allServices;
        private readonly Stack<SnapshotAction> _undoStack = new Stack<SnapshotAction>();
        private readonly Stack<SnapshotAction> _redoStack = new Stack<SnapshotAction>();
        private readonly UserAccount _currentAccount;
        private readonly IDisposable _statusSubscription;
        private readonly DispatcherTimer _patientStatusPollingTimer;
        private Dictionary<int, string> _knownPatientStatuses = new Dictionary<int, string>();
        private bool _hasUnreadAppointmentStatusNotification;
        private readonly string _patientStatusSnapshotPath;

        private sealed class SnapshotAction
        {
            public List<MedicalService> Before { get; set; }
            public List<MedicalService> After { get; set; }
        }

        public bool IsAdmin { get; private set; }
        public bool IsPatient => !IsAdmin;

        private ObservableCollection<MedicalService> _services;
        private MedicalService _selectedService;
        private string _searchText = "";
        private string _selectedCategory = AllCategoriesKey;
        private decimal _minPrice;
        private decimal _maxPrice = 3000;
        private string _sortOption = "Название";
        private Appointment _upcomingAppointment;
        private readonly UserProfile _currentUser;

        public ObservableCollection<MedicalService> Services
        {
            get => _services;
            set { _services = value; OnPropertyChanged(nameof(Services)); OnPropertyChanged(nameof(ServicesCount)); }
        }

        public int ServicesCount => _services?.Count ?? 0;

        public MedicalService SelectedService
        {
            get => _selectedService;
            set { _selectedService = value; OnPropertyChanged(nameof(SelectedService)); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); ApplyFilters(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); ApplyFilters(); }
        }

        public decimal MinPrice
        {
            get => _minPrice;
            set { _minPrice = value; OnPropertyChanged(nameof(MinPrice)); ApplyFilters(); }
        }

        public decimal MaxPrice
        {
            get => _maxPrice;
            set { _maxPrice = value; OnPropertyChanged(nameof(MaxPrice)); ApplyFilters(); }
        }

        public string SortOption
        {
            get => _sortOption;
            set { _sortOption = value; OnPropertyChanged(nameof(SortOption)); ApplyFilters(); }
        }

        public Appointment UpcomingAppointment
        {
            get => _upcomingAppointment;
            set
            {
                _upcomingAppointment = value;
                OnPropertyChanged(nameof(UpcomingAppointment));
                OnPropertyChanged(nameof(HasUpcomingAppointment));
                OnPropertyChanged(nameof(UpcomingAppointmentTitle));
                OnPropertyChanged(nameof(UpcomingAppointmentWhen));
            }
        }

        public bool HasUpcomingAppointment => _upcomingAppointment != null;

        public string UpcomingAppointmentTitle =>
            _upcomingAppointment == null
                ? string.Empty
                : $"{_upcomingAppointment.ServiceName}, {_upcomingAppointment.Doctor}";

        public string UpcomingAppointmentWhen
        {
            get
            {
                if (_upcomingAppointment == null) return string.Empty;
                return $"{_upcomingAppointment.AppointmentDate:dd.MM} {_upcomingAppointment.Time}";
            }
        }

        public string CurrentUserName => _currentUser?.FullName ?? "";
        public string CurrentUserAvatarPath => _currentUser?.AvatarPath;
        public bool HasCurrentUserAvatar => !string.IsNullOrWhiteSpace(CurrentUserAvatarPath);
        public bool HasUnreadAppointmentStatusNotification
        {
            get => _hasUnreadAppointmentStatusNotification;
            private set
            {
                if (_hasUnreadAppointmentStatusNotification == value) return;
                _hasUnreadAppointmentStatusNotification = value;
                OnPropertyChanged(nameof(HasUnreadAppointmentStatusNotification));
            }
        }
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public ObservableCollection<CategoryOption> Categories { get; } = new ObservableCollection<CategoryOption>();
        public List<string> SortOptions { get; } = new List<string> { "Название", "Цена (возр)", "Цена (уб)", "Рейтинг", "Длительность" };

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenBookingCommand { get; }
        public ICommand SwitchToRuCommand { get; }
        public ICommand SwitchToEnCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenProfileCommand { get; }
        public ICommand OpenAppointmentsCommand { get; }
        public ICommand OpenDbTablesCommand { get; }
        public ICommand OpenDoctorsCommand { get; }
        public ICommand OpenDoctorManagementCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public MainViewModel(UserAccount account)
        {
            _currentAccount = account ?? new UserAccount
            {
                Role = UserRole.Patient,
                DisplayName = "Пациент",
                Username = "patient"
            };

            IsAdmin = _currentAccount.Role == UserRole.Admin;
            _allServices = _service.LoadServices();
            _currentUser = new UserProfile
            {
                Id = _currentAccount.Id,
                Username = _currentAccount.Username,
                Role = _currentAccount.Role,
                FullName = string.IsNullOrWhiteSpace(_currentAccount.DisplayName)
                    ? _currentAccount.Username
                    : _currentAccount.DisplayName,
                Phone = _currentAccount.Phone ?? string.Empty,
                Email = _currentAccount.Email ?? string.Empty,
                AvatarPath = _currentAccount.AvatarPath
            };

            if (IsPatient)
            {
                _statusSubscription = AppointmentStatusSubject.Instance.Subscribe(this);
                _patientStatusSnapshotPath = BuildPatientStatusSnapshotPath();
                _knownPatientStatuses = GetCurrentPatientStatuses();
                _patientStatusPollingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _patientStatusPollingTimer.Tick += (_, __) => PollPatientStatusChanges();
                _patientStatusPollingTimer.Start();
            }

            AddCommand = new RelayCommand(_ => OpenAddWindow(), _ => IsAdmin);
            EditCommand = new RelayCommand(s => OpenEditWindow(s as MedicalService), s => s != null && IsAdmin);
            DeleteCommand = new RelayCommand(s => DeleteService(s as MedicalService), s => s != null && IsAdmin);
            OpenBookingCommand = new RelayCommand(s => OpenBooking(s as MedicalService), s => s != null && IsPatient);
            SwitchToRuCommand = new RelayCommand(_ => App.SwitchLanguage("ru"));
            SwitchToEnCommand = new RelayCommand(_ => App.SwitchLanguage("en"));
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            AboutCommand = new RelayCommand(_ => MessageBox.Show(
                "МедЦентр\nКурсовой проект\nАвтоматизация записи на приём",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information));
            RefreshCommand = new AsyncRelayCommand(async _ => await RefreshAsync());
            OpenProfileCommand = new RelayCommand(_ => OpenProfile());
            OpenAppointmentsCommand = new AsyncRelayCommand(async _ => await OpenAppointmentsAsync());
            OpenDbTablesCommand = new RelayCommand(_ => OpenDbTables(), _ => IsAdmin);
            OpenDoctorsCommand = new RelayCommand(_ => OpenDoctors());
            OpenDoctorManagementCommand = new RelayCommand(_ => OpenDoctorManagement(), _ => IsAdmin);
            UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);

            RebuildCategories();
            ApplyFilters();
            LoadUpcomingAppointment();
            InitializePatientNotificationState();

            App.LanguageChanged += () =>
            {
                RebuildCategories();
                OnPropertyChanged(nameof(Categories));
            };
        }

        private void OpenProfile()
        {
            var vm = new ProfileViewModel(_currentUser);
            var win = new ProfileWindow { DataContext = vm };
            AttachSafeOwner(win);

            if (win.ShowDialog() == true)
            {
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CurrentUserAvatarPath));
                OnPropertyChanged(nameof(HasCurrentUserAvatar));
            }
        }

        private void RebuildCategories()
        {
            Categories.Clear();
            Categories.Add(new CategoryOption(AllCategoriesKey, GetCategoryDisplay(AllCategoriesKey)));

            foreach (string cat in _allServices
                         .Select(s => s.Category)
                         .Where(c => !string.IsNullOrWhiteSpace(c))
                         .Distinct()
                         .OrderBy(c => c))
            {
                Categories.Add(new CategoryOption(cat, GetCategoryDisplay(cat)));
            }
        }

        public void ApplyFilters()
        {
            IEnumerable<MedicalService> result = _allServices;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLower();
                result = result.Where(s =>
                    (s.ShortName ?? "").ToLower().Contains(search) ||
                    (s.FullName ?? "").ToLower().Contains(search) ||
                    (s.Specialist ?? "").ToLower().Contains(search) ||
                    (s.Description ?? "").ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != AllCategoriesKey)
                result = result.Where(s => s.Category == SelectedCategory);

            result = result.Where(s => s.FinalPrice >= MinPrice && s.FinalPrice <= MaxPrice);

            switch (SortOption)
            {
                case "Цена (возр)": result = result.OrderBy(s => s.FinalPrice); break;
                case "Цена (уб)": result = result.OrderByDescending(s => s.FinalPrice); break;
                case "Рейтинг": result = result.OrderByDescending(s => s.Rating); break;
                case "Длительность": result = result.OrderBy(s => s.Duration); break;
                default: result = result.OrderBy(s => s.ShortName); break;
            }

            Services = new ObservableCollection<MedicalService>(result);
        }

        private void OpenAddWindow()
        {
            var before = CloneServices(_allServices);
            var vm = new AddEditViewModel(null);
            var win = new AddEditWindow { DataContext = vm };
            AttachSafeOwner(win);
            if (win.ShowDialog() == true)
            {
                try
                {
                    _service.AddOrUpdateService(vm.Service);
                    _allServices = _service.LoadServices();
                    RegisterHistory(before, _allServices);
                    RebuildCategories();
                    ApplyFilters();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось сохранить услугу: {ex.Message}", "Ошибка БД",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenEditWindow(MedicalService service)
        {
            if (service == null) return;
            var before = CloneServices(_allServices);
            var vm = new AddEditViewModel(service, readOnly: false);
            var win = new AddEditWindow { DataContext = vm };
            AttachSafeOwner(win);
            if (win.ShowDialog() == true)
            {
                try
                {
                    _service.AddOrUpdateService(service);
                    _allServices = _service.LoadServices();
                    RegisterHistory(before, _allServices);
                    ApplyFilters();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось обновить услугу: {ex.Message}", "Ошибка БД",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenBooking(MedicalService service)
        {
            if (service == null) return;
            var win = new BookingWindow(service, _currentUser.FullName, _currentUser.Phone, null);
            AttachSafeOwner(win);
            if (win.ShowDialog() == true)
                LoadUpcomingAppointment();
        }

        private void DeleteService(MedicalService service)
        {
            if (service == null) return;
            var before = CloneServices(_allServices);
            var res = MessageBox.Show($"Удалить услугу \"{service.ShortName}\"?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                _service.DeleteService(service.Id);
                _allServices = _service.LoadServices();
                RegisterHistory(before, _allServices);
                RebuildCategories();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить услугу. По данной услуге есть активные записи пациентов.\n" +
                                $"Сначала отмените или завершите все приёмы.\n\nДеталь: {ex.Message}",
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;

            var action = _undoStack.Pop();
            _redoStack.Push(new SnapshotAction
            {
                Before = CloneServices(action.Before),
                After = CloneServices(action.After)
            });

            _allServices = CloneServices(action.Before);
            PersistAllServices();
            RebuildCategories();
            ApplyFilters();
            NotifyUndoRedoChanged();
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            var action = _redoStack.Pop();
            _undoStack.Push(new SnapshotAction
            {
                Before = CloneServices(action.Before),
                After = CloneServices(action.After)
            });

            _allServices = CloneServices(action.After);
            PersistAllServices();
            RebuildCategories();
            ApplyFilters();
            NotifyUndoRedoChanged();
        }

        private void RegisterHistory(List<MedicalService> before, List<MedicalService> after)
        {
            _undoStack.Push(new SnapshotAction
            {
                Before = CloneServices(before),
                After = CloneServices(after)
            });

            while (_undoStack.Count > MaxUndoSteps)
            {
                var temp = _undoStack.Take(MaxUndoSteps).Reverse().ToList();
                _undoStack.Clear();
                foreach (var item in temp) _undoStack.Push(item);
            }

            _redoStack.Clear();
            NotifyUndoRedoChanged();
        }

        private void NotifyUndoRedoChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            CommandManager.InvalidateRequerySuggested();
        }

        private static List<MedicalService> CloneServices(IEnumerable<MedicalService> source)
        {
            return source.Select(CloneService).ToList();
        }

        private static MedicalService CloneService(MedicalService s)
        {
            return new MedicalService
            {
                Id = s.Id,
                ShortName = s.ShortName,
                FullName = s.FullName,
                Description = s.Description,
                Category = s.Category,
                Rating = s.Rating,
                Price = s.Price,
                Duration = s.Duration,
                Specialist = s.Specialist,
                SpecialistImage = s.SpecialistImage,
                Department = s.Department,
                IsPopular = s.IsPopular,
                HasDiscount = s.HasDiscount,
                DiscountPercent = s.DiscountPercent,
                PatientsCount = s.PatientsCount,
                Images = s.Images != null ? new List<string>(s.Images) : new List<string>(),
                TimeSlots = s.TimeSlots != null
                    ? s.TimeSlots.Select(t => new TimeSlot
                    {
                        Time = t.Time,
                        IsAvailable = t.IsAvailable,
                        DoctorName = t.DoctorName
                    }).ToList()
                    : new List<TimeSlot>()
            };
        }

        private void ClearFilters()
        {
            SearchText = "";
            SelectedCategory = AllCategoriesKey;
            MinPrice = 0;
            MaxPrice = 3000;
            ApplyFilters();
        }

        private static string GetCategoryDisplay(string categoryKey)
        {
            if (categoryKey == AllCategoriesKey)
                return (Application.Current.TryFindResource("Cat_All") as string) ?? "Все";

            string resourceKey = null;
            switch (categoryKey)
            {
                case "Консультация": resourceKey = "Cat_Consultation"; break;
                case "Диагностика": resourceKey = "Cat_Diagnostics"; break;
                case "Стоматология": resourceKey = "Cat_Dentistry"; break;
                case "Процедуры": resourceKey = "Cat_Procedures"; break;
                case "Физиотерапия": resourceKey = "Cat_Physiotherapy"; break;
            }

            if (resourceKey == null)
                return categoryKey;

            return (Application.Current.TryFindResource(resourceKey) as string) ?? categoryKey;
        }

        private void LoadUpcomingAppointment()
        {
            try
            {
                var patientKey = string.IsNullOrWhiteSpace(_currentUser?.Phone)
                    ? _currentUser?.FullName
                    : _currentUser.Phone;

                if (string.IsNullOrWhiteSpace(patientKey))
                {
                    UpcomingAppointment = null;
                    return;
                }

                var appointments = _service.GetAppointmentsForPatientAsync(patientKey).GetAwaiter().GetResult();
                var now = DateTime.Now;
                UpcomingAppointment = appointments
                    .Where(a => a.IsActive)
                    .Where(a => ComposeAppointmentDateTime(a) >= now)
                    .OrderBy(a => ComposeAppointmentDateTime(a))
                    .FirstOrDefault();
            }
            catch
            {
                UpcomingAppointment = null;
            }
        }

        private static DateTime ComposeAppointmentDateTime(Appointment appointment)
        {
            if (appointment == null) return DateTime.MinValue;
            if (TimeSpan.TryParseExact(appointment.Time ?? "", "hh\\:mm", CultureInfo.InvariantCulture, out var time))
                return appointment.AppointmentDate.Date.Add(time);
            return appointment.AppointmentDate;
        }

        private async Task OpenAppointmentsAsync()
        {
            var win = new AppointmentsWindow(_currentAccount);
            AttachSafeOwner(win);
            win.ShowDialog();
            HasUnreadAppointmentStatusNotification = false;
            _knownPatientStatuses = GetCurrentPatientStatuses();
            SaveCurrentPatientStatusSnapshot();
            await Task.Delay(10);
            LoadUpcomingAppointment();
        }

        public void RefreshPatientAppointmentNotificationsAfterBooking()
        {
            LoadUpcomingAppointment();
        }

        private void OpenDbTables()
        {
            var win = new DbTablesWindow();
            AttachSafeOwner(win);
            win.ShowDialog();
        }

        private void OpenDoctors()
        {
            var win = new DoctorsWindow(_currentUser?.FullName ?? "", _currentUser?.Phone ?? "");
            AttachSafeOwner(win);
            win.ShowDialog();
            LoadUpcomingAppointment();
        }

        private void OpenDoctorManagement()
        {
            var win = new DoctorManagementWindow();
            AttachSafeOwner(win);
            win.ShowDialog();
        }

        private static void AttachSafeOwner(Window child)
        {
            if (child == null)
                return;

            var owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w.IsLoaded && w != child);

            if (owner == null)
            {
                owner = Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsLoaded && w.Visibility == Visibility.Visible && w != child);
            }

            if (owner != null && owner != child)
            {
                child.Owner = owner;
            }
        }

        private void PersistAllServices()
        {
            var dbServices = _service.LoadServices();
            var desiredIds = new HashSet<int>(_allServices.Select(s => s.Id));
            foreach (var dbService in dbServices.Where(s => s.Id > 0 && !desiredIds.Contains(s.Id)))
            {
                try { _service.DeleteService(dbService.Id); }
                catch { /* keep undo/redo resilient — DB constraints can block removals for booked services */ }
            }

            foreach (var service in _allServices)
                _service.AddOrUpdateService(service);
        }

        private async Task RefreshAsync()
        {
            try
            {
                var category = SelectedCategory == AllCategoriesKey ? null : SelectedCategory;
                _allServices = await _service.SearchServicesAsync(SearchText, category, MinPrice, MaxPrice, SortOption);
                await Task.Delay(10);
                RebuildCategories();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось обновить данные: {ex.Message}", "Ошибка БД",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void OnNext(AppointmentStatusChangedEvent value)
        {
            if (!IsPatient || value == null)
                return;

            if (value.ChangedByRole != UserRole.Admin && value.ChangedByRole != UserRole.Doctor)
                return;

            var phone = NormalizePhone(_currentUser?.Phone);
            var eventPhone = NormalizePhone(value.PatientPhone);
            var name = NormalizeText(_currentUser?.FullName);
            var eventName = NormalizeText(value.PatientName);

            bool byPhone = !string.IsNullOrWhiteSpace(phone) && phone == eventPhone;
            bool byName = !string.IsNullOrWhiteSpace(name) && name == eventName;
            bool byAppointment = IsAppointmentOwnedByCurrentPatient(value.AppointmentId);

            if (byPhone || byName || byAppointment)
            {
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() => HasUnreadAppointmentStatusNotification = true);
                }
                else
                {
                    HasUnreadAppointmentStatusNotification = true;
                }
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var chars = phone.Where(char.IsDigit).ToArray();
            var digits = new string(chars);

            // Compare by the stable local part to avoid mismatches like +7/8 prefixes.
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            return digits;
        }

        private static string NormalizeText(string text)
        {
            return (text ?? string.Empty).Trim().ToUpperInvariant();
        }

        private bool IsAppointmentOwnedByCurrentPatient(int appointmentId)
        {
            if (appointmentId <= 0)
                return false;

            try
            {
                var patientKey = string.IsNullOrWhiteSpace(_currentUser?.Phone)
                    ? _currentUser?.FullName
                    : _currentUser.Phone;

                if (string.IsNullOrWhiteSpace(patientKey))
                    return false;

                var appointments = _service.GetAppointmentsForPatientAsync(patientKey).GetAwaiter().GetResult();
                return appointments.Any(a => a.Id == appointmentId);
            }
            catch
            {
                return false;
            }
        }

        private void InitializePatientNotificationState()
        {
            if (!IsPatient)
                return;

            try
            {
                var current = GetCurrentPatientStatuses();
                var saved = LoadPatientStatusSnapshot();

                if (saved.Count == 0)
                {
                    SavePatientStatusSnapshot(current);
                    HasUnreadAppointmentStatusNotification = false;
                    return;
                }

                HasUnreadAppointmentStatusNotification = HasStatusChanges(saved, current);
            }
            catch
            {
                HasUnreadAppointmentStatusNotification = false;
            }
        }

        private void PollPatientStatusChanges()
        {
            if (!IsPatient)
                return;

            try
            {
                var current = GetCurrentPatientStatuses();
                if (HasStatusChanges(_knownPatientStatuses, current))
                {
                    HasUnreadAppointmentStatusNotification = true;
                }

                _knownPatientStatuses = current;
            }
            catch
            {
                // polling failures should not break UI
            }
        }

        private void SaveCurrentPatientStatusSnapshot()
        {
            if (!IsPatient)
                return;

            try
            {
                SavePatientStatusSnapshot(GetCurrentPatientStatuses());
            }
            catch
            {
                // keep UI responsive even if snapshot saving fails
            }
        }

        private Dictionary<int, string> GetCurrentPatientStatuses()
        {
            var patientKey = string.IsNullOrWhiteSpace(_currentUser?.Phone)
                ? _currentUser?.FullName
                : _currentUser.Phone;

            if (string.IsNullOrWhiteSpace(patientKey))
                return new Dictionary<int, string>();

            var appointments = _service.GetAppointmentsForPatientAsync(patientKey).GetAwaiter().GetResult();
            return appointments.ToDictionary(a => a.Id, a => NormalizeText(a.Status));
        }

        private static bool HasStatusChanges(Dictionary<int, string> previous, Dictionary<int, string> current)
        {
            foreach (var item in current)
            {
                if (previous.TryGetValue(item.Key, out var oldStatus) &&
                    NormalizeText(oldStatus) != NormalizeText(item.Value))
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<int, string> LoadPatientStatusSnapshot()
        {
            var result = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(_patientStatusSnapshotPath) || !File.Exists(_patientStatusSnapshotPath))
                return result;

            foreach (var line in File.ReadAllLines(_patientStatusSnapshotPath))
            {
                var parts = line.Split(new[] { '|' }, 2);
                if (parts.Length != 2)
                    continue;

                if (!int.TryParse(parts[0], out var id))
                    continue;

                result[id] = NormalizeText(parts[1]);
            }

            return result;
        }

        private void SavePatientStatusSnapshot(Dictionary<int, string> statuses)
        {
            if (string.IsNullOrWhiteSpace(_patientStatusSnapshotPath))
                return;

            var directory = Path.GetDirectoryName(_patientStatusSnapshotPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var lines = statuses
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}|{NormalizeText(x.Value)}")
                .ToArray();

            File.WriteAllLines(_patientStatusSnapshotPath, lines);
        }

        private string BuildPatientStatusSnapshotPath()
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MedicalCenter");

            var userKey = NormalizeText(_currentUser?.Phone);
            if (string.IsNullOrWhiteSpace(userKey))
                userKey = NormalizeText(_currentUser?.Username);
            if (string.IsNullOrWhiteSpace(userKey))
                userKey = NormalizeText(_currentUser?.FullName);
            if (string.IsNullOrWhiteSpace(userKey))
                userKey = "patient";

            var safeKey = new string(userKey.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch).ToArray());
            return Path.Combine(baseDir, $"status-snapshot-{safeKey}.txt");
        }
    }
}
