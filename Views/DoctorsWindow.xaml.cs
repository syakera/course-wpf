using System.Windows;
using MedicalCenter.Models;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class DoctorsWindow : Window
    {
        private readonly string _patientName;
        private readonly string _patientPhone;

        public DoctorsWindow(string patientName = "", string patientPhone = "")
        {
            _patientName = patientName;
            _patientPhone = patientPhone;
            InitializeComponent();

            var vm = new DoctorsPageViewModel();
            vm.BookingRequested += OnBookingRequested;
            DataContext = vm;
        }

        private void OnBookingRequested(Doctor doctor, MedicalService service)
        {
            var win = new BookingWindow(service, _patientName, _patientPhone, doctor);
            win.Owner = this;
            if (win.ShowDialog() == true &&
                Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.RefreshPatientAppointmentNotificationsAfterBooking();
            }
        }
    }
}
