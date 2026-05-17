using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Services;

namespace MedicalCenter.Views
{
    public partial class DbTablesWindow : Window, INotifyPropertyChanged
    {
        private readonly MedicalServiceService _service = new MedicalServiceService();
        private string _tableKey = "MedicalServices";
        private DataTable _currentTable;

        public DataTable CurrentTable
        {
            get => _currentTable;
            set
            {
                _currentTable = value;
                OnPropertyChanged(nameof(CurrentTable));
            }
        }

        public string ActiveTableName
        {
            get
            {
                switch (_tableKey)
                {
                    case "Appointments":
                        return "Текущая таблица: Appointments";
                    case "Doctors":
                        return "Текущая таблица: Doctors";
                    case "AuditLog":
                        return "Текущая таблица: AuditLog";
                    default:
                        return "Текущая таблица: MedicalServices";
                }
            }
        }

        public ICommand ShowServicesTableCommand { get; }
        public ICommand ShowAppointmentsTableCommand { get; }
        public ICommand ShowDoctorsTableCommand { get; }
        public ICommand ShowAuditTableCommand { get; }
        public ICommand RefreshCommand { get; }

        public DbTablesWindow()
        {
            InitializeComponent();
            DataContext = this;

            ShowServicesTableCommand = new AsyncRelayCommand(async _ => await SetTableAsync("MedicalServices"));
            ShowAppointmentsTableCommand = new AsyncRelayCommand(async _ => await SetTableAsync("Appointments"));
            ShowDoctorsTableCommand = new AsyncRelayCommand(async _ => await SetTableAsync("Doctors"));
            ShowAuditTableCommand = new AsyncRelayCommand(async _ => await SetTableAsync("AuditLog"));
            RefreshCommand = new AsyncRelayCommand(async _ => await LoadCurrentAsync());

            _ = LoadCurrentAsync();
        }

        private async System.Threading.Tasks.Task SetTableAsync(string tableKey)
        {
            _tableKey = tableKey;
            OnPropertyChanged(nameof(ActiveTableName));
            await LoadCurrentAsync();
        }

        private async System.Threading.Tasks.Task LoadCurrentAsync()
        {
            CurrentTable = await _service.GetTableDataAsync(_tableKey);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
