using System.Windows;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class DoctorScheduleWindow : Window
    {
        public DoctorScheduleWindow(string doctorName)
        {
            InitializeComponent();
            DataContext = new DoctorScheduleViewModel(doctorName);
            Closing += DoctorScheduleWindow_Closing;
        }

        private void DoctorScheduleWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as DoctorScheduleViewModel;
            if (vm != null && vm.IsBusy)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "Дождитесь завершения текущей операции сохранения.",
                    "Сохранение данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
