using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;

namespace KLS;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Dictionary<string, string> associations = new(); // exeName → hexLayout
    private WinEventHook winEventHook;
    private string? lastExeName = null;
    private string initialLayoutHex;

    private const string AssociationsFile = "associations.json";
    private const uint KLF_ACTIVATE = 0x00000001;
    private const int VK_MENU = 0x12;   // Alt
    private const int VK_SHIFT = 0x10;  // Shift
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public MainWindow()
    {
        InitializeComponent();
        LoadKeyboardLayouts();
        LoadApplications();
        AssociateButton.Click += AssociateButton_Click;
        TestLayoutButton.Click += TestLayoutButton_Click;
        EditAssociationButton.Click += EditAssociationButton_Click;
        DeleteAssociationButton.Click += DeleteAssociationButton_Click;

        AppsListBox.SelectionChanged += AppsListBox_SelectionChanged;

        winEventHook = new WinEventHook();
        winEventHook.ForegroundWindowChanged += OnForegroundWindowChanged;

        LoadAssociations();

        // Enregistre le layout clavier au démarrage
        initialLayoutHex = GetCurrentLayoutHex();
        StatusText.Text = $"Layout clavier initial : {initialLayoutHex}";
    }

    private string GetCurrentLayoutHex()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(hwnd, out _);
        IntPtr hkl = GetKeyboardLayout(threadId);
        return ((uint)hkl.ToInt64()).ToString("X8");
    }

    private void SendAltShift()
    {
        keybd_event((byte)VK_MENU, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);   // Alt down
        keybd_event((byte)VK_SHIFT, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);  // Shift down
        keybd_event((byte)VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);    // Shift up
        keybd_event((byte)VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);     // Alt up
    }

    private void LoadKeyboardLayouts()
    {
        var layouts = InputLanguageManager.Current.AvailableInputLanguages.Cast<CultureInfo>().ToList();
        LayoutsListBox.ItemsSource = layouts;
    }

    private void LoadApplications()
    {
        var apps = Process.GetProcesses()
            .Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle))
            .OrderBy(p => p.ProcessName)
            .ToList();
        AppsListBox.ItemsSource = apps;
    }

    private void AssociateButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppsListBox.SelectedItem is Process app && LayoutsListBox.SelectedItem is CultureInfo layout)
        {
            associations[app.ProcessName.ToLower() + ".exe"] = GetLayoutHex(layout);
            UpdateAssociationsList();
            StatusText.Text = $"Association enregistrée : {app.ProcessName} → {GetLayoutHex(layout)}";
            SaveAssociations();
        }
    }

    private void UpdateAssociationsList()
    {
        AssociationsListBox.Items.Clear();
        foreach (var kvp in associations)
        {
            AssociationsListBox.Items.Add($"{kvp.Key} → {kvp.Value}");
        }
    }

    private void OnForegroundWindowChanged(string exeName)
    {
        if (exeName == lastExeName)
            return;

        lastExeName = exeName;

        if (associations.TryGetValue(exeName, out var wantedLayout))
        {
            string currentLayout = GetCurrentLayoutHex();
            if (!string.Equals(currentLayout, wantedLayout, StringComparison.OrdinalIgnoreCase))
            {
                SendAltShift();
                string newLayout = GetCurrentLayoutHex();
                StatusText.Text = $"Alt+Shift simulé (layout actuel: {currentLayout}, voulu: {wantedLayout})\nLayout après changement: {newLayout}";
                initialLayoutHex = newLayout; // Actualise le layout enregistré
            }
            else
            {
                StatusText.Text = $"Layout déjà correct ({wantedLayout}) pour {exeName}";
            }
        }
        else
        {
            string currentLayout = GetCurrentLayoutHex();
            if (!string.Equals(currentLayout, initialLayoutHex, StringComparison.OrdinalIgnoreCase))
            {
                SendAltShift();
                string newLayout = GetCurrentLayoutHex();
                StatusText.Text = $"Alt+Shift simulé (layout différent du démarrage)\nLayout après changement: {newLayout}";
                initialLayoutHex = newLayout; // Actualise le layout enregistré
            }
            else
            {
                StatusText.Text = $"Layout courant = layout initial ({initialLayoutHex})";
            }
        }
    }

    private void SaveAssociations()
    {
        var json = JsonSerializer.Serialize(associations, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AssociationsFile, json);
    }

    private void LoadAssociations()
    {
        associations.Clear();
        if (File.Exists(AssociationsFile))
        {
            var json = File.ReadAllText(AssociationsFile);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    associations[kvp.Key] = kvp.Value; // hexadécimal direct
                }
            }
        }
        UpdateAssociationsList();
    }

    private void TestLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (LayoutsListBox.SelectedItem is CultureInfo layout)
        {
            LoadKeyboardLayout(GetLayoutHex(layout), KLF_ACTIVATE);
            StatusText.Text = $"Layout global changé manuellement : {layout.DisplayName}";
        }
        else
        {
            StatusText.Text = "Sélectionnez un layout à tester.";
        }
    }

    private void EditAssociationButton_Click(object sender, RoutedEventArgs e)
    {
        if (AssociationsListBox.SelectedItem is string selected)
        {
            var parts = selected.Split('→');
            if (parts.Length == 2)
            {
                var exeName = parts[0].Trim();
                var app = AppsListBox.Items.OfType<Process>().FirstOrDefault(p => (p.ProcessName.ToLower() + ".exe") == exeName);
                if (app != null)
                    AppsListBox.SelectedItem = app;
                if (associations.TryGetValue(exeName, out var hexLayout))
                {
                    var layoutObj = LayoutsListBox.Items
                        .OfType<CultureInfo>()
                        .FirstOrDefault(c => GetLayoutHex(c) == hexLayout);
                    if (layoutObj != null)
                        LayoutsListBox.SelectedItem = layoutObj;
                }
            }
        }
    }

    private void DeleteAssociationButton_Click(object sender, RoutedEventArgs e)
    {
        if (AssociationsListBox.SelectedItem is string selected)
        {
            var exeName = selected.Split('→')[0].Trim();
            if (associations.Remove(exeName))
            {
                UpdateAssociationsList();
                SaveAssociations();
                StatusText.Text = $"Association supprimée : {exeName}";
            }
        }
    }

    private void AppsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppsListBox.SelectedItem is Process app)
        {
            var exeName = app.ProcessName.ToLower() + ".exe";
            if (associations.TryGetValue(exeName, out var wantedLayout))
            {
                string currentLayout = GetCurrentLayoutHex();
                if (!string.Equals(currentLayout, wantedLayout, StringComparison.OrdinalIgnoreCase))
                {
                    SendAltShift();
                    string newLayout = GetCurrentLayoutHex();
                    StatusText.Text = $"Alt+Shift simulé (layout actuel: {currentLayout}, voulu: {wantedLayout})\nLayout après changement: {newLayout}";
                    initialLayoutHex = newLayout; // Actualise le layout enregistré
                }
                else
                {
                    StatusText.Text = $"Layout déjà correct ({wantedLayout}) pour {exeName}";
                }
            }
            else
            {
                string currentLayout = GetCurrentLayoutHex();
                if (!string.Equals(currentLayout, initialLayoutHex, StringComparison.OrdinalIgnoreCase))
                {
                    SendAltShift();
                    string newLayout = GetCurrentLayoutHex();
                    StatusText.Text = $"Alt+Shift simulé (layout différent du démarrage)\nLayout après changement: {newLayout}";
                    initialLayoutHex = newLayout; // Actualise le layout enregistré
                }
                else
                {
                    StatusText.Text = $"Layout courant = layout initial ({initialLayoutHex})";
                }
            }
        }
    }

    private string GetLayoutHex(CultureInfo culture)
    {
        // Les codes hex sont généralement les LCID en 8 chiffres, ex: "0000040C" pour fr-FR
        return culture.KeyboardLayoutId.ToString("X8");
    }

    protected override void OnClosed(EventArgs e)
    {
        winEventHook?.Dispose();
        base.OnClosed(e);
    }
}