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
        }
    }
}
