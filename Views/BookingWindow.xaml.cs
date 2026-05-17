using System.Windows;
using MedicalCenter.Models;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class BookingWindow : Window
    {
        public BookingWindow(MedicalService service)
            : this(service, null, null, null)
        {
        }

        public BookingWindow(MedicalService service, string patientName, string patientPhone, Doctor preSelectedDoctor)
        {
            InitializeComponent();
            var vm = new BookingViewModel(service, patientName ?? string.Empty, patientPhone ?? string.Empty, preSelectedDoctor)
            {
                OwnerWindow = this
            };
            DataContext = vm;
        }
    }
}
