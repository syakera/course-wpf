using System;
using System.Windows;
using System.Windows.Controls;

namespace MedicalCenter.Controls
{
    public class MetricValueChangedEventArgs : RoutedEventArgs
    {
        public double OldValue { get; }
        public double NewValue { get; }

        public MetricValueChangedEventArgs(RoutedEvent routedEvent, object source, double oldValue, double newValue)
            : base(routedEvent, source)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public partial class MetricGaugeControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MetricGaugeControl),
                new FrameworkPropertyMetadata("Metric", FrameworkPropertyMetadataOptions.AffectsRender, OnAppearancePropertyChanged, CoerceTitle),
                ValidateTitle);

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(
                nameof(MaxValue),
                typeof(double),
                typeof(MetricGaugeControl),
                new FrameworkPropertyMetadata(200d, FrameworkPropertyMetadataOptions.AffectsRender, OnMaxValueChanged, CoerceMaxValue),
                ValidateMaxValue);

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(MetricGaugeControl),
                new FrameworkPropertyMetadata(90d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue),
                ValidateValue);

        public static readonly DependencyProperty AlertThresholdProperty =
            DependencyProperty.Register(
                nameof(AlertThreshold),
                typeof(double),
                typeof(MetricGaugeControl),
                new FrameworkPropertyMetadata(120d, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearancePropertyChanged, CoerceAlertThreshold),
                ValidateThreshold);

        public static readonly RoutedEvent PreviewMetricChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PreviewMetricChanged),
                RoutingStrategy.Tunnel,
                typeof(EventHandler<MetricValueChangedEventArgs>),
                typeof(MetricGaugeControl));

        public static readonly RoutedEvent ThresholdReachedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ThresholdReached),
                RoutingStrategy.Bubble,
                typeof(EventHandler<MetricValueChangedEventArgs>),
                typeof(MetricGaugeControl));

        public MetricGaugeControl()
        {
            InitializeComponent();
            Loaded += (_, __) => UpdateGauge();
            SizeChanged += (_, __) => UpdateGauge();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double AlertThreshold
        {
            get => (double)GetValue(AlertThresholdProperty);
            set => SetValue(AlertThresholdProperty, value);
        }

        public event EventHandler<MetricValueChangedEventArgs> PreviewMetricChanged
        {
            add => AddHandler(PreviewMetricChangedEvent, value);
            remove => RemoveHandler(PreviewMetricChangedEvent, value);
        }

        public event EventHandler<MetricValueChangedEventArgs> ThresholdReached
        {
            add => AddHandler(ThresholdReachedEvent, value);
            remove => RemoveHandler(ThresholdReachedEvent, value);
        }

        private static bool ValidateTitle(object value) =>
            value is string s && s.Trim().Length > 0 && s.Length <= 40;

        private static object CoerceTitle(DependencyObject d, object baseValue)
        {
            string s = ((string)baseValue).Trim();
            return s.Length == 0 ? "Metric" : s;
        }

        private static bool ValidateMaxValue(object value)
        {
            double v = (double)value;
            return !double.IsNaN(v) && !double.IsInfinity(v) && v > 0d && v <= 500d;
        }

        private static object CoerceMaxValue(DependencyObject d, object baseValue)
        {
            double max = (double)baseValue;
            var c = (MetricGaugeControl)d;
            max = Math.Max(10d, Math.Min(max, 500d));
            if (max < c.AlertThreshold)
                max = c.AlertThreshold;
            return max;
        }

        private static bool ValidateValue(object value)
        {
            double v = (double)value;
            return !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0d && v <= 500d;
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var c = (MetricGaugeControl)d;
            double v = (double)baseValue;
            return Math.Max(0d, Math.Min(v, c.MaxValue));
        }

        private static bool ValidateThreshold(object value)
        {
            double v = (double)value;
            return !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0d && v <= 500d;
        }

        private static object CoerceAlertThreshold(DependencyObject d, object baseValue)
        {
            var c = (MetricGaugeControl)d;
            double threshold = (double)baseValue;
            return Math.Max(0d, Math.Min(threshold, c.MaxValue));
        }

        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MetricGaugeControl)d).UpdateGauge();
        }

        private static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ValueProperty);
            d.CoerceValue(AlertThresholdProperty);
            ((MetricGaugeControl)d).UpdateGauge();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MetricGaugeControl)d;
            double oldValue = (double)e.OldValue;
            double newValue = (double)e.NewValue;

            var previewArgs = new MetricValueChangedEventArgs(PreviewMetricChangedEvent, control, oldValue, newValue);
            control.RaiseEvent(previewArgs);

            control.UpdateGauge();

            if (oldValue < control.AlertThreshold && newValue >= control.AlertThreshold)
            {
                var thresholdArgs = new MetricValueChangedEventArgs(ThresholdReachedEvent, control, oldValue, newValue);
                control.RaiseEvent(thresholdArgs);
            }
        }

        private void UpdateGauge()
        {
            if (FillBar == null)
                return;

            TitleText.Text = Title;
            ValueText.Text = $"{Math.Round(Value):0}/{Math.Round(MaxValue):0}";

            double hostWidth = Math.Max(1d, ActualWidth - 18d);
            double ratio = MaxValue <= 0 ? 0 : Value / MaxValue;
            FillBar.Width = hostWidth * Math.Max(0d, Math.Min(1d, ratio));
        }
    }
}
