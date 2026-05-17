using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MedicalCenter.Commands
{
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _executeAsync;
        private readonly Func<object, bool> _canExecute;
        private readonly Action<Exception> _onError;
        private bool _isExecuting;

        public AsyncRelayCommand(
            Func<object, Task> executeAsync,
            Func<object, bool> canExecute = null,
            Action<Exception> onError = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
            _onError = onError;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _executeAsync(parameter).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (_onError != null)
                {
                    _onError(ex);
                }
                else
                {
                    MessageBox.Show(
                        $"Непредвиденная ошибка: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
