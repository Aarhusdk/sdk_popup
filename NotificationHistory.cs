using System;
using System.Collections.Generic;
using System.Text;

namespace sdk_popup
{
    /// <summary>
    /// Fixed-size ring buffer for recent notifications.
    /// Used by the history feature (v1.5.0).
    /// </summary>
    internal class NotificationHistory
    {
        private readonly LinkedList<HistoryEntry> _entries = new LinkedList<HistoryEntry>();
        private int _maxSize = 50;

        public int MaxSize
        {
            get { return _maxSize; }
            set
            {
                _maxSize = Math.Max(1, value);
                TrimToMax();
            }
        }

        public int Count
        {
            get { return _entries.Count; }
        }

        public void Add(string title, string message, string type, string reason)
        {
            var entry = new HistoryEntry
            {
                Timestamp = DateTime.Now,
                Title = title ?? "",
                Message = message ?? "",
                Type = type ?? "",
                Reason = reason ?? ""
            };

            _entries.AddFirst(entry);
            TrimToMax();
        }

        /// <summary>
        /// Returns entry at index (0 = newest) as JSON. Empty string if out of range.
        /// </summary>
        public string GetJson(int index)
        {
            if (index < 0 || index >= _entries.Count) return "";

            int i = 0;
            foreach (var e in _entries)
            {
                if (i == index) return ToJson(e);
                i++;
            }
            return "";
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private void TrimToMax()
        {
            while (_entries.Count > _maxSize)
            {
                _entries.RemoveLast();
            }
        }

        private static string ToJson(HistoryEntry e)
        {
            var sb = new StringBuilder(256);
            sb.Append("{");
            AppendField(sb, "timestamp", e.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"), true);
            AppendField(sb, "title", e.Title, false);
            AppendField(sb, "message", e.Message, false);
            AppendField(sb, "type", e.Type, false);
            AppendField(sb, "reason", e.Reason, false);
            sb.Append("}");
            return sb.ToString();
        }

        private static void AppendField(StringBuilder sb, string key, string value, bool first)
        {
            if (!first) sb.Append(",");
            sb.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private class HistoryEntry
        {
            public DateTime Timestamp;
            public string Title;
            public string Message;
            public string Type;
            public string Reason;
        }
    }
}
