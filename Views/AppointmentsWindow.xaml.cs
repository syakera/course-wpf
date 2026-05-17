using System.Windows;
using MedicalCenter.Models;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class AppointmentsWindow : Window
    {
        private readonly AppointmentsViewModel _viewModel;

        public AppointmentsWindow(UserAccount account)
        {
            InitializeComponent();

            var safeAccount = account ?? new UserAccount
            {
                Role = UserRole.Patient,
                Username = "patient",
                DisplayName = "Пациент",
                Phone = string.Empty
            };

            _viewModel = new AppointmentsViewModel(
                safeAccount.Role,
                safeAccount.DisplayName ?? safeAccount.Username ?? string.Empty,
                safeAccount.Phone ?? string.Empty);

            DataContext = _viewModel;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel?.StopAutoRefresh();
            base.OnClosed(e);
        }
    }
}