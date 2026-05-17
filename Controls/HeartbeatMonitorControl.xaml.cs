using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MedicalCenter.Controls
{
    public partial class HeartbeatMonitorControl : UserControl
    {
        public static readonly DependencyProperty DotSizeProperty =
            DependencyProperty.Register(
                nameof(DotSize),
                typeof(double),
                typeof(HeartbeatMonitorControl),
                new FrameworkPropertyMetadata(6d, FrameworkPropertyMetadataOptions.AffectsRender, OnDotSizeChanged, CoerceDotSize),
                ValidateDotSize);

        public static readonly DependencyProperty PulseScaleProperty =
            DependencyProperty.Register(
                nameof(PulseScale),
                typeof(double),
                typeof(HeartbeatMonitorControl),
                new FrameworkPropertyMetadata(1.5d, FrameworkPropertyMetadataOptions.AffectsRender, OnPulseScaleChanged, CoercePulseScale),
                ValidatePulseScale);

        public static readonly DependencyProperty AnimationSecondsProperty =
            DependencyProperty.Register(
                nameof(AnimationSeconds),
                typeof(double),
                typeof(HeartbeatMonitorControl),
                new FrameworkPropertyMetadata(1.1d, FrameworkPropertyMetadataOptions.AffectsRender, OnAnimationSecondsChanged, CoerceAnimationSeconds),
                ValidateAnimationSeconds);

        public static readonly RoutedEvent PulseClickedDirectEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PulseClickedDirect),
                RoutingStrategy.Direct,
                typeof(RoutedEventHandler),
                typeof(HeartbeatMonitorControl));

        public static readonly RoutedEvent BeatBroadcastEvent =
            EventManager.RegisterRoutedEvent(
                nameof(BeatBroadcast),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(HeartbeatMonitorControl));

        public HeartbeatMonitorControl()
        {
            InitializeComponent();
            Loaded += (_, __) => StartAnimations();
            Unloaded += (_, __) => StopAnimations();
            ApplyVisualSettings();
        }

        public double DotSize
        {
            get => (double)GetValue(DotSizeProperty);
            set => SetValue(DotSizeProperty, value);
        }

        public double PulseScale
        {
            get => (double)GetValue(PulseScaleProperty);
            set => SetValue(PulseScaleProperty, value);
        }

        public double AnimationSeconds
        {
            get => (double)GetValue(AnimationSecondsProperty);
            set => SetValue(AnimationSecondsProperty, value);
        }

        public event RoutedEventHandler PulseClickedDirect
        {
            add => AddHandler(PulseClickedDirectEvent, value);
            remove => RemoveHandler(PulseClickedDirectEvent, value);
        }

        public event RoutedEventHandler BeatBroadcast
        {
            add => AddHandler(BeatBroadcastEvent, value);
            remove => RemoveHandler(BeatBroadcastEvent, value);
        }

        private static bool ValidateDotSize(object value)
        {
            double d = (double)value;
            return !double.IsNaN(d) && !double.IsInfinity(d) && d >= 4d && d <= 16d;
        }

        private static object CoerceDotSize(DependencyObject d, object baseValue)
        {
            var control = (HeartbeatMonitorControl)d;
            double size = (double)baseValue;
            double maxAllowed = Math.Max(4d, (control.ActualHeight > 0 ? control.ActualHeight : 38d) - 8d);
            return Math.Max(4d, Math.Min(size, maxAllowed));
        }

        private static void OnDotSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((HeartbeatMonitorControl)d).ApplyVisualSettings();
            d.CoerceValue(PulseScaleProperty);
        }

        private static bool ValidatePulseScale(object value)
        {
            double d = (double)value;
            return !double.IsNaN(d) && !double.IsInfinity(d) && d >= 1d && d <= 3d;
        }

        private static object CoercePulseScale(DependencyObject d, object baseValue)
        {
            var control = (HeartbeatMonitorControl)d;
            double scale = (double)baseValue;
            double maxByDotSize = 1.0d + (12d / Math.Max(4d, control.DotSize));
            return Math.Max(1d, Math.Min(scale, maxByDotSize));
        }

        private static void OnPulseScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HeartbeatMonitorControl)d;
            control.ApplyVisualSettings();
            control.RestartAnimations();
        }

        private static bool ValidateAnimationSeconds(object value)
        {
            double d = (double)value;
            return !double.IsNaN(d) && !double.IsInfinity(d) && d >= 0.4d && d <= 5d;
        }

        private static object CoerceAnimationSeconds(DependencyObject d, object baseValue)
        {
            var control = (HeartbeatMonitorControl)d;
            double seconds = (double)baseValue;
            if (control.PulseScale > 2.2d)
                seconds = Math.Max(seconds, 0.8d);
            return Math.Min(5d, Math.Max(0.4d, seconds));
        }

        private static void OnAnimationSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HeartbeatMonitorControl)d;
            control.ApplyVisualSettings();
            control.RestartAnimations();
        }

        private void ApplyVisualSettings()
        {
            if (PulseDot == null)
                return;

            PulseDot.Width = DotSize;
            PulseDot.Height = DotSize;
            PulseDot.Margin = new Thickness(0, 1, 4, 0);

            if (PulseDot.RenderTransform is ScaleTransform st)
            {
                st.CenterX = DotSize / 2d;
                st.CenterY = DotSize / 2d;
            }
        }

        private void RestartAnimations()
        {
            if (!IsLoaded)
                return;

            StartAnimations();
        }

        private void StartAnimations()
        {
            if (PulseDot == null || EcLine == null)
                return;

            StopAnimations();

            var duration = new Duration(TimeSpan.FromSeconds(AnimationSeconds));

            if (PulseDot.RenderTransform is ScaleTransform st)
            {
                var pulseScale = new DoubleAnimation(1.0, PulseScale, duration)
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };

                st.BeginAnimation(ScaleTransform.ScaleXProperty, pulseScale);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, pulseScale.Clone());
            }

            var pulseOpacity = new DoubleAnimation(0.65, 0.98, duration)
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            PulseDot.BeginAnimation(OpacityProperty, pulseOpacity);

            var lineShift = new DoubleAnimation(6.0, 0.0, duration)
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            EcLine.BeginAnimation(Shape.StrokeDashOffsetProperty, lineShift);
        }

        private void StopAnimations()
        {
            if (PulseDot?.RenderTransform is ScaleTransform st)
            {
                st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                st.ScaleX = 1;
                st.ScaleY = 1;
            }

            PulseDot?.BeginAnimation(OpacityProperty, null);

            if (EcLine != null)
            {
                EcLine.BeginAnimation(Shape.StrokeDashOffsetProperty, null);
                EcLine.StrokeDashOffset = 0;
            }
        }

        private void PulseDot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PulseClickedDirectEvent, this));
            RaiseEvent(new RoutedEventArgs(BeatBroadcastEvent, this));
            e.Handled = true;
        }
    }
}
