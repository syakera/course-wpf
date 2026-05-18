using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MedicalCenter.Commands;
using MedicalCenter.Models;

namespace MedicalCenter.ViewModels
{
    public class AddEditViewModel : INotifyPropertyChanged
    {
        public MedicalService Service { get; }
        public bool IsEditMode { get; }
        public bool IsReadOnly { get; }
        public bool IsNotReadOnly => !IsReadOnly;

        public List<string> Categories { get; } = new List<string>
        {
            "Консультация", "Диагностика", "Стоматология", "Процедуры", "Физиотерапия", "Хирургия"
        };

        public List<string> Departments { get; } = new List<string>
        {
            "Терапия", "Кардиология", "Неврология", "Стоматология", "Физиотерапия", "Диагностика", "Хирургия"
        };

        public string TimeSlotsText
        {
            get => Service.TimeSlots != null && Service.TimeSlots.Count > 0
                ? string.Join("; ", Service.TimeSlots.Select(t => t.Time))
                : "09:00;10:00;11:00;12:00;13:00;14:00;15:00;16:00;17:00";
            set
            {
                var times = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                Service.TimeSlots = times.Select(t => new TimeSlot { Time = t, IsAvailable = true }).ToList();
                OnPropertyChanged(nameof(TimeSlotsText));
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddEditViewModel(MedicalService existing, bool readOnly = false)
        {
            IsReadOnly = readOnly;

            if (existing != null)
            {
                Service = existing;
                IsEditMode = true;
            }
            else
            {
                Service = new MedicalService { Rating = 4.0, Duration = 30, Price = 1000 };
                IsEditMode = false;
            }

            SaveCommand = new RelayCommand(Save, _ => !IsReadOnly && CanSave());
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanSave() =>
            !string.IsNullOrWhiteSpace(Service.ShortName) &&
            !string.IsNullOrWhiteSpace(Service.Specialist) &&
            Service.Price > 0;

        private void Save(object param)
        {
            if (param is Window win)
                win.DialogResult = true;
        }

        private void Cancel(object param)
        {
            if (param is Window win)
                win.DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}