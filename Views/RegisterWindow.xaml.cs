using System.Windows;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _viewModel = new RegisterViewModel();

        public string RegisteredUsername { get; private set; }

        public RegisterWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            RegisterButton.Click += RegisterButton_Click;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            _viewModel.Username = (UsernameBox.Text ?? string.Empty).Trim();
            _viewModel.FullName = (FullNameBox.Text ?? string.Empty).Trim();
            _viewModel.Phone = (PhoneBox.Text ?? string.Empty).Trim();
            _viewModel.Email = (EmailBox.Text ?? string.Empty).Trim();
            _viewModel.Password = PasswordBox.Password ?? string.Empty;
            _viewModel.ConfirmPassword = ConfirmPasswordBox.Password ?? string.Empty;

            _viewModel.RegisterCommand.Execute(null);

            if (_viewModel.IsRegistered)
            {
                RegisteredUsername = _viewModel.Username;
                DialogResult = true;
                return;
            }

            if (_viewModel.HasError)
            {
                ShowError(_viewModel.ErrorMessage);
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = string.Empty;
        }
    }
}
