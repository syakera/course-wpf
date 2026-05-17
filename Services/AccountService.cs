using System;
using System.Linq;
using System.Text.RegularExpressions;
using MedicalCenter.Data.Entities;
using MedicalCenter.Data.UnitOfWork;
using MedicalCenter.Models;

namespace MedicalCenter.Services
{
    public class AccountService
    {
        public UserAccount Authenticate(string username, string password)
        {
            var login = (username ?? string.Empty).Trim();
            var pass = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(pass))
                return null;

            using (IUnitOfWork uow = new UnitOfWork())
            {
                var entity = uow.Users
                    .GetByCredentialsAsync(login, pass)
                    .GetAwaiter()
                    .GetResult();
                return entity == null ? null : ToModel(entity);
            }
        }

        public void RegisterPatient(string username, string password, string fullName,
            string phone = "", string email = "")
        {
            var login = (username ?? string.Empty).Trim();
            var pass = password ?? string.Empty;
            var displayName = (fullName ?? string.Empty).Trim();
            var phoneValue = (phone ?? string.Empty).Trim();
            var emailValue = (email ?? string.Empty).Trim();

            if (!Regex.IsMatch(login, @"^(?:[A-Za-z]{2,}|[А-Яа-яЁё]{2,})$"))
                throw new InvalidOperationException(
                    "Имя пользователя: только кириллица или латиница, минимум 2 символа.");

            if (pass.Length < 8)
                throw new InvalidOperationException(
                    "Пароль должен содержать не менее 8 символов.");

            if (!Regex.IsMatch(displayName, @"^[А-Яа-яЁё\s-]{2,}$"))
                throw new InvalidOperationException(
                    "ФИО: только кириллица, пробелы и дефис, минимум 2 символа.");

            if (!string.IsNullOrEmpty(phoneValue) &&
                !Regex.IsMatch(phoneValue, @"^\+375(\(?(?:25|29|33|44)\)?\d{3}-?\d{2}-?\d{2})$"))
                throw new InvalidOperationException(
                    "Телефон должен быть в формате +375(XX)XXX-XX-XX.");

            if (!string.IsNullOrEmpty(emailValue) &&
                !Regex.IsMatch(emailValue, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"))
                throw new InvalidOperationException(
                    "Введите корректный email (пример: name@example.com).");

            using (IUnitOfWork uow = new UnitOfWork())
            {
                var exists = uow.Users.Query().Any(x => x.Username == login);
                if (exists)
                    throw new InvalidOperationException("Пользователь с таким логином уже существует.");

                uow.Users.Add(new UserEntity
                {
                    Username = login,
                    Password = pass,
                    Role = (int)UserRole.Patient,
                    DisplayName = displayName,
                    Phone = phoneValue,
                    Email = emailValue,
                    AvatarPath = ""
                });
                uow.SaveChanges();
            }
        }

        public void UpdateUserProfile(UserAccount account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));

            using (IUnitOfWork uow = new UnitOfWork())
            {
                var entity = uow.Users.Query().FirstOrDefault(u => u.Id == account.Id);
                if (entity == null)
                    throw new InvalidOperationException("Учётная запись не найдена.");

                entity.DisplayName = account.DisplayName;
                entity.Phone = account.Phone;
                entity.Email = account.Email;
                entity.AvatarPath = account.AvatarPath;
                uow.SaveChanges();
            }
        }

        public UserAccount GetProfile(int userId)
        {
            using (IUnitOfWork uow = new UnitOfWork())
            {
                var entity = uow.Users.Query().FirstOrDefault(u => u.Id == userId);
                return entity == null ? null : ToModel(entity);
            }
        }

        /// <summary>
        /// Builds the login for a freshly created doctor account from their surname.
        /// </summary>
        public string BuildDoctorLogin(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "doctor";

            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "doctor" : parts[0];
        }

        private static UserAccount ToModel(UserEntity entity)
        {
            return new UserAccount
            {
                Id = entity.Id,
                Username = entity.Username,
                Role = (UserRole)entity.Role,
                DisplayName = entity.DisplayName,
                Phone = entity.Phone,
                Email = entity.Email,
                AvatarPath = entity.AvatarPath
            };
        }
    }
}
