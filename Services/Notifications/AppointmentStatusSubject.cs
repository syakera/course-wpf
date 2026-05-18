using System;
using System.Collections.Generic;
using System.Linq;
using MedicalCenter.Models;

namespace MedicalCenter.Services.Notifications
{
    public sealed class AppointmentStatusChangedEvent
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public string NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public UserRole ChangedByRole { get; set; }
    }

    public sealed class AppointmentStatusSubject : IObservable<AppointmentStatusChangedEvent>
    {
        private readonly List<IObserver<AppointmentStatusChangedEvent>> _observers =
            new List<IObserver<AppointmentStatusChangedEvent>>();
        private readonly object _sync = new object();

        private AppointmentStatusSubject()
        {
        }

        public static AppointmentStatusSubject Instance { get; } = new AppointmentStatusSubject();

        public IDisposable Subscribe(IObserver<AppointmentStatusChangedEvent> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            lock (_sync)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
            }

            return new Unsubscriber(_observers, observer, _sync);
        }

        public void Notify(AppointmentStatusChangedEvent evt)
        {
            if (evt == null) return;

            List<IObserver<AppointmentStatusChangedEvent>> snapshot;
            lock (_sync)
            {
                snapshot = _observers.ToList();
            }

            foreach (var observer in snapshot)
                observer.OnNext(evt);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<AppointmentStatusChangedEvent>> _observers;
            private readonly IObserver<AppointmentStatusChangedEvent> _observer;
            private readonly object _sync;

            public Unsubscriber(
                List<IObserver<AppointmentStatusChangedEvent>> observers,
                IObserver<AppointmentStatusChangedEvent> observer,
                object sync)
            {
                _observers = observers;
                _observer = observer;
                _sync = sync;
            }

            public void Dispose()
            {
                lock (_sync)
                {
                    if (_observer != null && _observers.Contains(_observer))
                        _observers.Remove(_observer);
                }
            }
        }
    }
}
