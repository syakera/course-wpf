using System;
using System.Windows;
using System.Windows.Input;

namespace MedicalCenter.Views
{
    public partial class AddEditWindow : Window
    {
        public AddEditWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => ApplyCursor();
        }

        private void ApplyCursor()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/cursor.cur");
                var info = Application.GetResourceStream(uri);
                if (info != null)
                    Cursor = new Cursor(info.Stream);
            }
            catch { }
        }
    }
}