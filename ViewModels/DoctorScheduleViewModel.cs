using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MedicalCenter.Commands;
using MedicalCenter.Models;
using MedicalCenter.Services;

namespace MedicalCenter.ViewModels
{
    public class DoctorScheduleViewModel : INotifyPropertyChanged
    {
        private readonly MedicalServiceService _service = new MedicalServiceService();
        private readonly string _doctorName;

        // ── Undo / Redo stacks ────────────────────────────────────────────
        private struct StatusChange
        {
            public int    AppointmentId;
            public string OldStatus;
            public string NewStatus;
            public string PatientName;
        }

        private readonly Stack<StatusChange> _undoStack = new Stack<StatusChange>();
        private readonly Stack<StatusChange> _redoStack = new Stack<StatusChange>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // ─────────────────────────────────────────────────────────────────

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
            }
        }

        public string DoctorName => _doctorName;

        public ObservableCollection<Appointment> Appointments { get; } = new ObservableCollection<Appointment>();

        private Appointment _selectedAppointment;
        public Appointment SelectedAppointment
        {
            get => _selectedAppointment;
            set
            {
                _selectedAppointment = value;
                OnPropertyChanged(nameof(SelectedAppointment));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand MarkCompletedCommand { get; }
        public ICommand MarkNoShowCommand { get; }
        public ICommand AddPrescriptionCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public DoctorScheduleViewModel(string doctorName)
        {
            _doctorName = string.IsNullOrWhiteSpace(doctorName) ? "Врач" : doctorName;
            RefreshCommand        = new AsyncRelayCommand(async _ => await LoadAsync());
            MarkCompletedCommand  = new AsyncRelayCommand(async _ => await UpdateStatusAsync("Завершена"),  _ => SelectedAppointment != null);
            MarkNoShowCommand     = new AsyncRelayCommand(async _ => await UpdateStatusAsync("Не явился"),  _ => SelectedAppointment != null);
            AddPrescriptionCommand = new AsyncRelayCommand(async _ => await AddPrescriptionAsync(),
                _ => SelectedAppointment != null && SelectedAppointment.Status == "Завершена");
            UndoCommand = new AsyncRelayCommand(async _ => await UndoAsync(), _ => CanUndo);
            RedoCommand = new AsyncRelayCommand(async _ => await RedoAsync(), _ => CanRedo);
            _ = LoadAsync();
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            var items = await _service.GetAppointmentsForDoctorAsync(_doctorName, SelectedDate.Date, SelectedDate.Date).ConfigureAwait(true);
            Appointments.Clear();
            foreach (var item in items.OrderBy(a => a.Time))
                Appointments.Add(item);
            OnPropertyChanged(nameof(Appointments));
        }

        private async System.Threading.Tasks.Task UpdateStatusAsync(string newStatus)
        {
            if (SelectedAppointment == null) return;

            var res = MessageBox.Show(
                $"Изменить статус приёма пациента «{SelectedAppointment.PatientName}» на «{newStatus}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            var change = new StatusChange
            {
                AppointmentId = SelectedAppointment.Id,
                OldStatus     = SelectedAppointment.Status,
                NewStatus     = newStatus,
                PatientName   = SelectedAppointment.PatientName
            };

            await _service.UpdateAppointmentStatusAsync(change.AppointmentId, newStatus).ConfigureAwait(true);

            _undoStack.Push(change);
            _redoStack.Clear();
            NotifyUndoRedo();

            await LoadAsync().ConfigureAwait(true);
        }

        private async System.Threading.Tasks.Task UndoAsync()
        {
            if (!CanUndo) return;
            var change = _undoStack.Pop();

            var res = MessageBox.Show(
                $"Отменить изменение статуса «{change.NewStatus}» → «{change.OldStatus}» для пациента «{change.PatientName}»?",
                "Отмена действия", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
            {
                _undoStack.Push(change);
                return;
            }

            await _service.UpdateAppointmentStatusAsync(change.AppointmentId, change.OldStatus).ConfigureAwait(true);
            _redoStack.Push(change);
            NotifyUndoRedo();
            await LoadAsync().ConfigureAwait(true);
        }

        private async System.Threading.Tasks.Task RedoAsync()
        {
            if (!CanRedo) return;
            var change = _redoStack.Pop();

            var res = MessageBox.Show(
                $"Повторить изменение статуса «{change.OldStatus}» → «{change.NewStatus}» для пациента «{change.PatientName}»?",
                "Повтор действия", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
            {
                _redoStack.Push(change);
                return;
            }

            await _service.UpdateAppointmentStatusAsync(change.AppointmentId, change.NewStatus).ConfigureAwait(true);
            _undoStack.Push(change);
            NotifyUndoRedo();
            await LoadAsync().ConfigureAwait(true);
        }

        private void NotifyUndoRedo()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            CommandManager.InvalidateRequerySuggested();
        }

        private async System.Threading.Tasks.Task AddPrescriptionAsync()
        {
            if (SelectedAppointment == null || SelectedAppointment.Status != "Завершена")
                return;

            var text = PromptForPrescriptionText();
            if (string.IsNullOrWhiteSpace(text))
                return;

            await _service.AddMedicalNoteAsync(SelectedAppointment.Id, "Назначение", text.Trim(), _doctorName)
                .ConfigureAwait(true);
            MessageBox.Show("Назначение сохранено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string PromptForPrescriptionText()
        {
            var inputBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 120,
                Margin = new Thickness(0, 10, 0, 10),
                MaxLength = MedicalNoteLimits.MaxContentLength
            };

            var okButton = new Button { Content = "Сохранить", Width = 90, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancelButton = new Button { Content = "Отмена", Width = 90, IsCancel = true };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var layout = new StackPanel { Margin = new Thickness(16) };
            layout.Children.Add(new TextBlock { Text = "Введите назначение для завершенного приема:" });
            layout.Children.Add(new TextBlock
            {
                Text = $"Не более {MedicalNoteLimits.MaxContentLength} символов.",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            });
            layout.Children.Add(inputBox);
            layout.Children.Add(buttons);

            var dialog = new Window
            {
                Title = "Новое назначение",
                Width = 460,
                Height = 290,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Content = layout,
                Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };

            okButton.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            cancelButton.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };

            var result = dialog.ShowDialog();
            return result == true ? inputBox.Text : null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
