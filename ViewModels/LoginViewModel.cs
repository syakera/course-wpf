using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;
using MedicalCenter.Views;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MedicalCenter.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AccountService _accountService = new AccountService();
        private string _username = "";
        private string _password = "";
        private string _errorMessage = "";

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); ErrorMessage = ""; }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); ErrorMessage = ""; }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => TryLogin());
        }

        private void TryLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите логин и пароль.";
                return;
            }

            UserAccount account;
            try
            {
                account = _accountService.Authenticate(Username, Password);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка БД: {ex.Message}";
                return;
            }

            if (account == null)
            {
                ErrorMessage = "Неверный логин или пароль.";
                return;
            }

            OpenMainWindow(account);
        }

        private void OpenMainWindow(UserAccount account)
        {
            if (account.Role == UserRole.Doctor)
            {
                var schedule = new DoctorScheduleWindow(account.DisplayName ?? account.Username);
                Application.Current.MainWindow = schedule;
                schedule.Show();
            }
            else
            {
                var mainWindow = new MainWindow(account);
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }

            foreach (Window win in Application.Current.Windows)
            {
                if (win.DataContext == this)
                {
                    win.Close();
                    return;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}