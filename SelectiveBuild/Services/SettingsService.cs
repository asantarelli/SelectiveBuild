using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SelectiveBuild.Services
{
    /// <summary>
    /// Persists user settings (last selected apps, last configuration) in AppData.
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsPath;
        private Dictionary<string, string> _settings;

        public SettingsService()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SelectiveBuild");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "settings.txt");
            _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Load();
        }

        public string Get(string key)
        {
            string v;
            return _settings.TryGetValue(key ?? "", out v) ? v : null;
        }

        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _settings[key] = value ?? "";
            Save();
        }

        public string[] GetList(string key)
        {
            var v = Get(key);
            if (string.IsNullOrEmpty(v)) return new string[0];
            return v.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public void SetList(string key, IEnumerable<string> values)
        {
            Set(key, string.Join("|", values));
        }

        private void Load()
        {
            _settings.Clear();
            if (!File.Exists(_settingsPath)) return;
            try
            {
                foreach (var line in File.ReadAllLines(_settingsPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq > 0) _settings[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
            catch { }
        }

        private void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# SelectiveBuild Settings");
                sb.AppendLine("# Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();
                foreach (var kv in _settings) sb.AppendLine(kv.Key + "=" + kv.Value);
                File.WriteAllText(_settingsPath, sb.ToString());
            }
            catch { }
        }
    }
}
