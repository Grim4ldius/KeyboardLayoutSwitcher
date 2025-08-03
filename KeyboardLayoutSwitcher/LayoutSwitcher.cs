using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KeyboardLayoutSwitcher
{
    public class LayoutSwitcher
    {
        private static readonly string LayoutsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.json");
        public Dictionary<string, string> AppLayouts { get; private set; } = new Dictionary<string, string>();

        public LayoutSwitcher()
        {
            LoadLayouts();
        }

        public void LoadLayouts()
        {
            if (File.Exists(LayoutsFile))
            {
                string json = File.ReadAllText(LayoutsFile);
                AppLayouts = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            else
            {
                AppLayouts = new Dictionary<string, string>();
            }
        }

        public void SaveLayouts()
        {
            string json = JsonSerializer.Serialize(AppLayouts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LayoutsFile, json);
        }

        public void CheckAndSwitch(string exeName)
        {
            if (AppLayouts.TryGetValue(exeName, out string layoutHex))
            {
                KeyboardManager.SwitchLayout(layoutHex);
            }
        }
    }
}