using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KeyboardLayoutSwitcher
{
    public partial class MainWindow : Window
    {
        private WinEventHook winEventHook;
        private readonly LayoutSwitcher layoutSwitcher;
        public ObservableCollection<AppLayoutItem> AppLayouts { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            layoutSwitcher = new LayoutSwitcher();
            winEventHook = new WinEventHook();
            winEventHook.ForegroundWindowChanged += OnForegroundWindowChanged;

            AppLayouts = new ObservableCollection<AppLayoutItem>(
                layoutSwitcher.AppLayouts.Select(kvp => new AppLayoutItem { Application = kvp.Key, Layout = kvp.Value })
            );
            AppsGrid.ItemsSource = AppLayouts;

            RefreshRunningApps();
        }

        private void OnForegroundWindowChanged(string exeName)
        {
            layoutSwitcher.CheckAndSwitch(exeName);
        }

        private void RefreshRunningApps()
        {
            RunningAppsCombo.Items.Clear();
            var processes = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLower() + ".exe")
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            foreach (var proc in processes)
                RunningAppsCombo.Items.Add(proc);
        }

        private void AddAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (RunningAppsCombo.SelectedItem is string appName && !string.IsNullOrWhiteSpace(appName))
            {
                AppLayouts.Add(new AppLayoutItem { Application = appName, Layout = "00000409 (QWERTY US)" });
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            layoutSwitcher.AppLayouts.Clear();
            foreach (var item in AppLayouts)
            {
                var layoutHex = item.Layout.Split(' ')[0];
                layoutSwitcher.AppLayouts[item.Application] = layoutHex;
            }
            layoutSwitcher.SaveLayouts();
            MessageBox.Show("Associations sauvegardées !");
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            layoutSwitcher.LoadLayouts();
            AppLayouts.Clear();
            foreach (var kvp in layoutSwitcher.AppLayouts)
            {
                AppLayouts.Add(new AppLayoutItem { Application = kvp.Key, Layout = kvp.Value });
            }
            RefreshRunningApps();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppsGrid.SelectedItem is AppLayoutItem item)
            {
                AppLayouts.Remove(item);
            }
        }

        private void TestSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            KeyboardManager.SwitchLayout("00000409"); // QWERTY US
        }

        private void TestSwitchFrButton_Click(object sender, RoutedEventArgs e)
        {
            KeyboardManager.SwitchLayout("0000040C"); // AZERTY FR
        }
    }

    public class AppLayoutItem
    {
        public string Application { get; set; }
        public string Layout { get; set; }
    }
}