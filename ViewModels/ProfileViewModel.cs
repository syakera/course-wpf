using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private const string PhonePattern = @"^\+375(\(?(?:25|29|33|44)\)?\d{3}-?\d{2}-?\d{2})$";
        private const string EmailPattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

        private readonly UserProfile _profile;
        private readonly AccountService _accountService = new AccountService();

        private string _fullName;
        private string _phone;
        private string _email;
        private string _selectedLanguage;

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

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(nameof(SelectedLanguage)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApplyLanguageCommand { get; }

        public ProfileViewModel(UserProfile profile)
        {
            _profile = profile;

            FullName = profile.FullName;
            Phone = profile.Phone;
            Email = profile.Email;
            SelectedLanguage = App.CurrentLanguageCode;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ApplyLanguageCommand = new RelayCommand(_ => App.SwitchLanguage(SelectedLanguage));
        }

        private void Save(object parameter)
        {
            var normalizedPhone = (Phone ?? string.Empty).Trim();
            var normalizedEmail = (Email ?? string.Empty).Trim();

            if (!string.IsNullOrEmpty(normalizedPhone) && !Regex.IsMatch(normalizedPhone, PhonePattern))
            {
                MessageBox.Show("Телефон должен быть в формате +375(XX)XXX-XX-XX.", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(normalizedEmail) && !Regex.IsMatch(normalizedEmail, EmailPattern))
            {
                MessageBox.Show("Введите корректный email (пример: name@example.com).", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _accountService.UpdateUserProfile(new UserAccount
                {
                    Id = _profile.Id,
                    Username = _profile.Username,
                    Role = _profile.Role,
                    DisplayName = FullName,
                    Phone = normalizedPhone,
                    Email = normalizedEmail
                });

                _profile.FullName = FullName;
                _profile.Phone = normalizedPhone;
                _profile.Email = normalizedEmail;

                App.SwitchLanguage(SelectedLanguage);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить профиль: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (parameter is Window win)
            {
                win.DialogResult = true;
                win.Close();
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window win)
            {
                win.DialogResult = false;
                win.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
