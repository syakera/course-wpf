using System;
using System.Globalization;

namespace MedicalCenter.Models
{
    public static class AppointmentScheduling
    {
        public static bool TryGetSlotStart(DateTime day, string slot, out DateTime start)
        {
            start = day;
            if (string.IsNullOrWhiteSpace(slot))
                return false;

            if (TimeSpan.TryParseExact(slot, "hh\\:mm", CultureInfo.InvariantCulture, out var time))
            {
                start = day.Date.Add(time);
                return true;
            }

            return false;
        }
    }
}
