using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MedicalCenter.Services;
using MedicalCenter.Views;  

namespace MedicalCenter
{
    public partial class App : Application
    {
        private static ResourceDictionary _currentLanguage;
        private static readonly Uri RuDictUri = new Uri("Strings/Strings.ru.xaml", UriKind.Relative);
        private static readonly Uri EnDictUri = new Uri("Strings/Strings.en.xaml", UriKind.Relative);
        public static event Action LanguageChanged;
        public static string CurrentLanguageCode { get; private set; } = "ru";

        public static void SwitchLanguage(string langCode)
        {
            if (_currentLanguage == null)
            {
                _currentLanguage = Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d?.Source != null &&
                                         (d.Source.OriginalString.EndsWith("Strings.ru.xaml", StringComparison.OrdinalIgnoreCase) ||
                                          d.Source.OriginalString.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase)));
            }

            var dict = new ResourceDictionary();

            switch (langCode)
            {
                case "en":
                    dict.Source = EnDictUri;
                    CurrentLanguageCode = "en";
                    break;
                default:
                    dict.Source = RuDictUri;
                    CurrentLanguageCode = "ru";
                    break;
            }

            if (_currentLanguage != null)
                Current.Resources.MergedDictionaries.Remove(_currentLanguage);

            Current.Resources.MergedDictionaries.Add(dict);
            _currentLanguage = dict;

            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(CurrentLanguageCode);
            LanguageChanged?.Invoke();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDir);

                // Force DB initialization on app startup.
                _ = new MedicalServiceService();

                SwitchLanguage("ru");

                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Необработанная ошибка интерфейса: {e.Exception.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"Критическая ошибка приложения: {ex?.Message ?? "Unknown error"}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Ошибка фоновой задачи: {e.Exception.GetBaseException().Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.SetObserved();
        }
    }
}