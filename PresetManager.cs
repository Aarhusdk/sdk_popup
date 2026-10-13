using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace sdk_popup
{
    /// <summary>
    /// Persistent named-preset storage for notification settings (v1.5.0).
    /// Saves to %APPDATA%\sdk_popup\presets.json.
    ///
    /// Uses a minimal hand-rolled JSON parser to avoid taking a dependency on
    /// System.Web.Extensions (not always available in lightweight deployments).
    /// </summary>
    internal class PresetManager
    {
        private readonly string _filePath;
        private Dictionary<string, Preset> _presets = new Dictionary<string, Preset>(StringComparer.OrdinalIgnoreCase);

        public PresetManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "sdk_popup");
            try { Directory.CreateDirectory(dir); } catch { }
            _filePath = Path.Combine(dir, "presets.json");
            Load();
        }

        public void Save(string name, Preset p)
        {
            if (string.IsNullOrEmpty(name) || p == null) return;
            _presets[name.Trim()] = p;
            Persist();
        }

        public Preset Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Preset p;
            return _presets.TryGetValue(name.Trim(), out p) ? p : null;
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!_presets.Remove(name.Trim())) return false;
            Persist();
            return true;
        }

        public string ListNames()
        {
            var keys = new List<string>(_presets.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(",", keys.ToArray());
        }

        #region Persistence

        private void Load()
        {
            _presets.Clear();
            try
            {
                if (!File.Exists(_filePath)) return;
                string json = File.ReadAllText(_filePath);
                Parse(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("sdk_popup PresetManager.Load error: " + ex.Message);
            }
        }

        private void Persist()
        {
            try
            {
                File.WriteAllText(_filePath, Serialize());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("sdk_popup PresetManager.Persist error: " + ex.Message);
            }
        }

        #endregion

        #region JSON serialize

        private string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            bool firstKey = true;
            foreach (var kvp in _presets)
            {
                if (!firstKey) sb.Append(",\n");
                firstKey = false;
                sb.Append("  \"").Append(Escape(kvp.Key)).Append("\": ");
                AppendPreset(sb, kvp.Value);
            }
            sb.Append("\n}");
            return sb.ToString();
        }

        private void AppendPreset(StringBuilder sb, Preset p)
        {
            sb.Append("{");
            AppendString(sb, "style", p.Style, true);
            AppendString(sb, "type", p.Type, false);
            AppendString(sb, "position", p.Position, false);
            AppendInt(sb, "duration", p.Duration);
            AppendInt(sb, "opacity", p.Opacity);
            AppendBool(sb, "pinned", p.Pinned);
            AppendString(sb, "bgColor", p.BgColor, false);
            AppendString(sb, "textColor", p.TextColor, false);
            AppendString(sb, "soundPreset", p.SoundPreset, false);
            AppendBool(sb, "soundEnabled", p.SoundEnabled);
            AppendBool(sb, "hoverPause", p.HoverPause);
            sb.Append("}");
        }

        private void AppendString(StringBuilder sb, string key, string value, bool first)
        {
            if (!first) sb.Append(",");
            sb.Append("\"").Append(key).Append("\":\"").Append(Escape(value ?? "")).Append("\"");
        }

        private void AppendInt(StringBuilder sb, string key, int value)
        {
            sb.Append(",\"").Append(key).Append("\":").Append(value);
        }

        private void AppendBool(StringBuilder sb, string key, bool value)
        {
            sb.Append(",\"").Append(key).Append("\":").Append(value ? "true" : "false");
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

        #endregion

        #region Minimal JSON parser (object-of-object)

        /// <summary>
        /// Parses the persisted shape: { "name": { "key": value, ... }, ... }.
        /// Kept intentionally small and forgiving - skips unknown keys.
        /// </summary>
        private void Parse(string json)
        {
            var parser = new MiniJson(json);
            var root = parser.ParseValue() as Dictionary<string, object>;
            if (root == null) return;

            foreach (var kvp in root)
            {
                var dict = kvp.Value as Dictionary<string, object>;
                if (dict == null) continue;
                var p = DictToPreset(dict);
                _presets[kvp.Key] = p;
            }
        }

        private Preset DictToPreset(Dictionary<string, object> d)
        {
            return new Preset
            {
                Style        = GetString(d, "style", "Default"),
                Type         = GetString(d, "type", "Info"),
                Position     = GetString(d, "position", "TopRight"),
                Duration     = GetInt(d, "duration", 3000),
                Opacity      = GetInt(d, "opacity", 100),
                Pinned       = GetBool(d, "pinned", false),
                BgColor      = GetString(d, "bgColor", ""),
                TextColor    = GetString(d, "textColor", ""),
                SoundPreset  = GetString(d, "soundPreset", ""),
                SoundEnabled = GetBool(d, "soundEnabled", false),
                HoverPause   = GetBool(d, "hoverPause", true)
            };
        }

        internal static string GetString(Dictionary<string, object> d, string key, string defVal)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is string s) return s;
            return defVal;
        }

        internal static int GetInt(Dictionary<string, object> d, string key, int defVal)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v))
            {
                if (v is double dv) return (int)dv;
                if (v is long lv) return (int)lv;
                if (v is int iv) return iv;
                if (v is string sv && int.TryParse(sv, out int result)) return result;
            }
            return defVal;
        }

        internal static bool GetBool(Dictionary<string, object> d, string key, bool defVal)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v))
            {
                if (v is bool bv) return bv;
                if (v is string sv) return sv == "true" || sv == "1";
                if (v is double dv) return dv != 0;
            }
            return defVal;
        }

        #endregion

        internal class Preset
        {
            public string Style = "Default";
            public string Type = "Info";
            public string Position = "TopRight";
            public int Duration = 3000;
            public int Opacity = 100;
            public bool Pinned;
            public string BgColor = "";
            public string TextColor = "";
            public string SoundPreset = "";
            public bool SoundEnabled;
            public bool HoverPause = true;
        }
    }
}
