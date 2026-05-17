using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MedicalCenter.Controls
{
    public partial class AppointmentBadgeControl : UserControl
    {
        public static readonly DependencyProperty DoctorNameProperty =
            DependencyProperty.Register(
                nameof(DoctorName),
                typeof(string),
                typeof(AppointmentBadgeControl),
                new FrameworkPropertyMetadata("Без имени", FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged, CoerceDoctorName),
                v => v is string s && s.Trim().Length > 0 && s.Length <= 60);

        public static readonly DependencyProperty AppointmentTimeProperty =
            DependencyProperty.Register(
                nameof(AppointmentTime),
                typeof(DateTime),
                typeof(AppointmentBadgeControl),
                new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged, CoerceAppointmentTime),
                v => v is DateTime);

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(AppointmentBadgeControl),
                new FrameworkPropertyMetadata("Ожидает", FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged, CoerceStatus),
                ValidateStatus);

        public static readonly RoutedEvent AppointmentClickedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(AppointmentClicked),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(AppointmentBadgeControl));

        public AppointmentBadgeControl()
        {
            InitializeComponent();
            MouseLeftButtonUp += (_, __) =>
                RaiseEvent(new RoutedEventArgs(AppointmentClickedEvent, this));
        }

        public string DoctorName
        {
            get => (string)GetValue(DoctorNameProperty);
            set => SetValue(DoctorNameProperty, value);
        }

        public DateTime AppointmentTime
        {
            get => (DateTime)GetValue(AppointmentTimeProperty);
            set => SetValue(AppointmentTimeProperty, value);
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public string AppointmentTimeString =>
            AppointmentTime.ToString("dd.MM HH:mm");

        public Brush StatusBrush =>
            Status == "Подтв." || Status == "Подтверждена" || Status == "Завершена"
                ? (Brush)FindResource("AccentBrush")
                : Status == "Отменена"
                    ? (Brush)FindResource("WarningBrush")
                    : (Brush)FindResource("BorderBrush");

        public event RoutedEventHandler AppointmentClicked
        {
            add => AddHandler(AppointmentClickedEvent, value);
            remove => RemoveHandler(AppointmentClickedEvent, value);
        }

        private static bool ValidateStatus(object value)
        {
            if (!(value is string s))
                return false;

            s = s.Trim();
            return s == "Ожидает" || s == "Подтв." || s == "Подтверждена" || s == "Завершена" || s == "Отменена";
        }

        private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // binding сам обновит визуал; дополнительная логика не требуется
        }

        private static object CoerceDoctorName(DependencyObject d, object baseValue)
        {
            var s = ((string)baseValue).Trim();
            return string.IsNullOrEmpty(s) ? "Без имени" : s;
        }

        private static object CoerceAppointmentTime(DependencyObject d, object baseValue)
        {
            var dt = (DateTime)baseValue;
            if (dt == DateTime.MinValue)
                dt = DateTime.Now;
            return dt;
        }

        private static object CoerceStatus(DependencyObject d, object baseValue)
        {
            var s = ((string)baseValue).Trim();
            if (s == "Подтверждена")
                s = "Подтв.";

            if (s != "Ожидает" && s != "Подтв." && s != "Завершена" && s != "Отменена")
                s = "Ожидает";
            return s;
        }
    }
}

