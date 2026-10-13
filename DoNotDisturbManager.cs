using System;

namespace sdk_popup
{
    /// <summary>
    /// Tracks Do Not Disturb state: manual toggle, timer-based, and scheduled windows (v1.5.0).
    /// All times are evaluated in the local timezone (DateTime.Now).
    /// </summary>
    internal class DoNotDisturbManager
    {
        private bool _manual;
        private DateTime? _timerUntil;       // null = no timer
        private int _scheduleStartMin = -1;  // -1 = no schedule
        private int _scheduleEndMin = -1;

        public bool Manual
        {
            get { return _manual; }
            set { _manual = value; }
        }

        public void SetTimer(int minutes)
        {
            if (minutes <= 0)
            {
                _timerUntil = null;
                return;
            }
            _timerUntil = DateTime.Now.AddMinutes(minutes);
        }

        public void SetSchedule(string startHHMM, string endHHMM)
        {
            _scheduleStartMin = ParseHHMM(startHHMM);
            _scheduleEndMin = ParseHHMM(endHHMM);
        }

        public void ClearSchedule()
        {
            _scheduleStartMin = -1;
            _scheduleEndMin = -1;
        }

        /// <summary>
        /// Returns true if DND is active right now from any of: manual, timer, schedule.
        /// </summary>
        public bool IsActive()
        {
            if (_manual) return true;

            if (_timerUntil.HasValue)
            {
                if (DateTime.Now < _timerUntil.Value) return true;
                // Timer expired - auto-clear
                _timerUntil = null;
            }

            if (_scheduleStartMin >= 0 && _scheduleEndMin >= 0)
            {
                if (IsInScheduledWindow()) return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the reason DND is active: "manual", "timer", "scheduled", or "" if not active.
        /// </summary>
        public string GetReason()
        {
            if (_manual) return "manual";

            if (_timerUntil.HasValue && DateTime.Now < _timerUntil.Value) return "timer";

            if (_scheduleStartMin >= 0 && _scheduleEndMin >= 0 && IsInScheduledWindow())
                return "scheduled";

            return "";
        }

        private bool IsInScheduledWindow()
        {
            var now = DateTime.Now;
            int nowMin = now.Hour * 60 + now.Minute;

            if (_scheduleStartMin == _scheduleEndMin) return false;

            if (_scheduleStartMin < _scheduleEndMin)
            {
                // Same-day window (e.g. 09:00-17:00)
                return nowMin >= _scheduleStartMin && nowMin < _scheduleEndMin;
            }

            // Overnight window (e.g. 22:00-07:00)
            return nowMin >= _scheduleStartMin || nowMin < _scheduleEndMin;
        }

        /// <summary>
        /// Parses HH:MM or HHMM. Returns -1 if invalid/empty.
        /// </summary>
        private static int ParseHHMM(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return -1;

            s = s.Trim().Replace(".", ":");
            int colonIdx = s.IndexOf(':');
            int h, m;

            try
            {
                if (colonIdx > 0)
                {
                    h = int.Parse(s.Substring(0, colonIdx));
                    m = int.Parse(s.Substring(colonIdx + 1));
                }
                else if (s.Length == 4)
                {
                    h = int.Parse(s.Substring(0, 2));
                    m = int.Parse(s.Substring(2, 2));
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }

            if (h < 0 || h > 23 || m < 0 || m > 59) return -1;
            return h * 60 + m;
        }
    }
}
