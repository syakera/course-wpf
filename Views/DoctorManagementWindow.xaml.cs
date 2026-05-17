using System.Windows;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class DoctorManagementWindow : Window
    {
        public DoctorManagementWindow()
        {
            InitializeComponent();
            DataContext = new DoctorManagementViewModel();
        }
    }
}
