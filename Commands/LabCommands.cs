using System.Windows.Input;

namespace MedicalCenter.Commands
{
    public static class LabCommands
    {
        public static readonly RoutedUICommand OpenAppointmentsCommand =
            new RoutedUICommand(
                "Open appointments window",
                nameof(OpenAppointmentsCommand),
                typeof(LabCommands),
                new InputGestureCollection
                { 
                    new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift)
                });
    }
}
