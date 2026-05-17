using System.Windows;
using MedicalCenter.Models;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(UserAccount account)
        {
            InitializeComponent();
            DataContext = new MainViewModel(account);
        }
    }
}
