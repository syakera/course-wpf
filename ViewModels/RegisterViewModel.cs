using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private readonly AccountService _accountService = new AccountService();
        private string _username;
        private string _fullName;
        private string _phone;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;
        private bool _isRegistered;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool IsRegistered
        {
            get => _isRegistered;
            private set
            {
                _isRegistered = value;
                OnPropertyChanged(nameof(IsRegistered));
            }
        }

        public ICommand RegisterCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(_ => Register());
        }

        private void Register()
        {
            IsRegistered = false;
            ErrorMessage = string.Empty;

            var username = (Username ?? string.Empty).Trim();
            var fullName = (FullName ?? string.Empty).Trim();
            var phone = (Phone ?? string.Empty).Trim();
            var email = (Email ?? string.Empty).Trim();
            var password = Password ?? string.Empty;
            var confirmPassword = ConfirmPassword ?? string.Empty;

            if (!Regex.IsMatch(username, @"^(?:[A-Za-z]{2,}|[А-Яа-яЁё]{2,})$"))
            {
                ErrorMessage = "Имя пользователя: только кириллица или латиница, минимум 2 символа.";
                return;
            }

            if (!Regex.IsMatch(fullName, @"^[А-Яа-яЁё\s-]{2,}$"))
            {
                ErrorMessage = "ФИО: только кириллица, пробелы и дефис, минимум 2 символа.";
                return;
            }

            if (!Regex.IsMatch(phone, @"^\+375(\(?(?:25|29|33|44)\)?\d{3}-?\d{2}-?\d{2})$"))
            {
                ErrorMessage = "Телефон: +375(XX)XXX-XX-XX или +375XXXXXXXXX.";
                return;
            }

            if (!Regex.IsMatch(email, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"))
            {
                ErrorMessage = "Введите корректный email (пример: name@example.com).";
                return;
            }

            if (password.Length < 8)
            {
                ErrorMessage = "Пароль должен содержать минимум 8 символов.";
                return;
            }

            if (password != confirmPassword)
            {
                ErrorMessage = "Пароли не совпадают.";
                return;
            }

            try
            {
                _accountService.RegisterPatient(username, password, fullName, phone, email);
                Username = username;
                IsRegistered = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
