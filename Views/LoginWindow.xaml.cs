using System.Windows;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel = new LoginViewModel();

        public LoginWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            LoginButton.Click += LoginButton_Click;
            RegisterButton.Click += RegisterButton_Click;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            var username = (UsernameBox.Text ?? string.Empty).Trim();
            var password = PasswordBox.Password ?? string.Empty;
            _viewModel.Username = username;
            _viewModel.Password = password;
            _viewModel.LoginCommand.Execute(null);

            if (_viewModel.HasError)
            {
                ShowError(_viewModel.ErrorMessage);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new RegisterWindow { Owner = this };
            if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(win.RegisteredUsername))
            {
                UsernameBox.Text = win.RegisteredUsername;
                PasswordBox.Focus();
                ShowError("Регистрация выполнена. Введите пароль и войдите.");
                ErrorBorder.Background = System.Windows.Media.Brushes.DarkSlateBlue;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;
        }
    }
}
