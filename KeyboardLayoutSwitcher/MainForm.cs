using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace KeyboardLayoutSwitcher
{
    public class MainForm : Form
    {
        private readonly LayoutSwitcher layoutSwitcher;
        private readonly DataGridView grid;
        private readonly Button saveButton;
        private readonly Button reloadButton;
        private readonly Button deleteButton;
        private readonly Label titleLabel;
        private readonly Label infoLabel;
        private readonly TableLayoutPanel mainPanel;
        private readonly ComboBox runningAppsCombo;
        private readonly Button addAppButton;
        private readonly StatusStrip statusStrip;
        private readonly ToolTip toolTip;
        private WinEventHook? winEventHook;
        private CheckBox enableSwitcherCheckBox;

        public MainForm()
        {
            Width = 700;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = System.Drawing.Color.White;
            Text = "Keyboard Layout Switcher";

            layoutSwitcher = new LayoutSwitcher();
            toolTip = new ToolTip();

            mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 11, // Augmente le nombre de lignes si besoin
                ColumnCount = 1,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Titre
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Info
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // CheckBox
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // ComboBox
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 220)); // Grid
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Save
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Reload
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Delete
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); // Hauteur suffisante pour les boutons test
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Ligne pour les boutons test

            titleLabel = new Label
            {
                Text = "Associations Application / Disposition clavier",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                BackColor = System.Drawing.Color.White
            };

            infoLabel = new Label
            {
                Text = "Ajoutez ou modifiez les associations ci-dessous.\nExemple de layout : 00000409 (QWERTY US), 0000040C (AZERTY FR)",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 10),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                BackColor = System.Drawing.Color.White
            };

            enableSwitcherCheckBox = new CheckBox
            {
                Text = "Activer le changement automatique du clavier",
                Dock = DockStyle.Fill,
                Checked = true,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            enableSwitcherCheckBox.CheckedChanged += EnableSwitcherCheckBox_CheckedChanged;

            runningAppsCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            RefreshRunningApps();
            toolTip.SetToolTip(runningAppsCombo, "Sélectionnez une application en cours d'exécution");

            addAppButton = new Button
            {
                Text = "Ajouter l'application sélectionnée",
                Dock = DockStyle.Fill,
                Height = 30,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            addAppButton.Click += AddAppButton_Click;
            toolTip.SetToolTip(addAppButton, "Ajoute l'application sélectionnée à la liste");

            var layoutColumn = new DataGridViewComboBoxColumn
            {
                HeaderText = "Layout",
                Width = 220,
                FlatStyle = FlatStyle.Flat,
                Items = { "00000409 (QWERTY US)", "0000040C (AZERTY FR)" }
            };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30,
                Margin = new Padding(5)
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Application (.exe)", Width = 220 });
            grid.Columns.Add(layoutColumn);
            grid.DataError += (s, e) => { e.ThrowException = false; };
            toolTip.SetToolTip(grid, "Liste des associations application / disposition clavier");

            foreach (var kvp in layoutSwitcher.AppLayouts)
            {
                var comboItems = new[] { "00000409 (QWERTY US)", "0000040C (AZERTY FR)" };
                string layoutValue = kvp.Value;
                string displayValue = comboItems.FirstOrDefault(item => item.StartsWith(layoutValue));
                if (displayValue == null)
                {
                    displayValue = layoutValue;
                    if (!layoutColumn.Items.Contains(displayValue))
                        layoutColumn.Items.Add(displayValue);
                }
                grid.Rows.Add(kvp.Key, displayValue);
            }

            saveButton = new Button
            {
                Text = "Sauvegarder",
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            saveButton.Click += SaveButton_Click;
            toolTip.SetToolTip(saveButton, "Sauvegarde les associations dans le fichier");

            reloadButton = new Button
            {
                Text = "Recharger",
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            reloadButton.Click += ReloadButton_Click;
            toolTip.SetToolTip(reloadButton, "Recharge les associations depuis le fichier");

            deleteButton = new Button
            {
                Text = "Supprimer la ligne sélectionnée",
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            deleteButton.Click += DeleteButton_Click;
            toolTip.SetToolTip(deleteButton, "Supprime la ligne sélectionnée dans la liste");
            deleteButton.Enabled = false;

            grid.SelectionChanged += (s, e) =>
            {
                deleteButton.Enabled = grid.SelectedRows.Count > 0 && !grid.SelectedRows[0].IsNewRow;
            };

            statusStrip = new StatusStrip();
            var statusLabel = new ToolStripStatusLabel { Text = "Prêt", Spring = true };
            statusStrip.Items.Add(statusLabel);

            mainPanel.Controls.Add(titleLabel, 0, 0);
            mainPanel.Controls.Add(infoLabel, 0, 1);
            mainPanel.Controls.Add(enableSwitcherCheckBox, 0, 2);

            var appAddPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Height = 35,
                BackColor = System.Drawing.Color.White
            };
            appAddPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            appAddPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            appAddPanel.Controls.Add(runningAppsCombo, 0, 0);
            appAddPanel.Controls.Add(addAppButton, 1, 0);

            mainPanel.Controls.Add(appAddPanel, 0, 3);

            mainPanel.Controls.Add(grid, 0, 5);

            var saveReloadPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Height = 40,
                BackColor = System.Drawing.Color.White
            };
            saveReloadPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            saveReloadPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            saveReloadPanel.Controls.Add(saveButton, 0, 0);
            saveReloadPanel.Controls.Add(reloadButton, 1, 0);

            mainPanel.Controls.Add(saveReloadPanel, 0, 6);

            mainPanel.Controls.Add(deleteButton, 0, 8);
            mainPanel.Controls.Add(statusStrip, 0, 9);

            Controls.Add(mainPanel);

            winEventHook = new WinEventHook();
            winEventHook.ForegroundWindowChanged += OnForegroundWindowChanged;

            var testButtonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = System.Drawing.Color.White
            };
            testButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            testButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var testSwitchButton = new Button
            {
                Text = "Tester QWERTY US",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            testSwitchButton.Click += TestSwitchButton_Click;
            toolTip.SetToolTip(testSwitchButton, "Change le clavier en QWERTY US (00000409)");

            var testSwitchFrButton = new Button
            {
                Text = "Tester AZERTY FR",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            testSwitchFrButton.Click += TestSwitchFrButton_Click;
            toolTip.SetToolTip(testSwitchFrButton, "Change le clavier en AZERTY FR (0000040C)");

            testButtonsPanel.Controls.Add(testSwitchButton, 0, 0);
            testButtonsPanel.Controls.Add(testSwitchFrButton, 1, 0);

            mainPanel.Controls.Add(testButtonsPanel, 0, 10); // 10 = index de la dernière ligne
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            layoutSwitcher.AppLayouts.Clear();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var key = row.Cells[0].Value?.ToString();
                var value = row.Cells[1].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    var layoutHex = value.Split(' ')[0];
                    layoutSwitcher.AppLayouts[key] = layoutHex;
                }
            }
            layoutSwitcher.SaveLayouts();
            MessageBox.Show("Associations sauvegardées !");
        }

        private void ReloadButton_Click(object? sender, EventArgs e)
        {
            layoutSwitcher.LoadLayouts();
            grid.Rows.Clear();
            var localLayoutComboColumn = grid.Columns[1] as DataGridViewComboBoxColumn;
            var comboItems = new[] { "00000409 (QWERTY US)", "0000040C (AZERTY FR)" };

            foreach (var kvp in layoutSwitcher.AppLayouts)
            {
                string layoutValue = kvp.Value;
                string displayValue = comboItems.FirstOrDefault(item => item.StartsWith(layoutValue));
                if (displayValue == null)
                {
                    displayValue = layoutValue;
                    if (localLayoutComboColumn != null && !localLayoutComboColumn.Items.Contains(displayValue))
                        localLayoutComboColumn.Items.Add(displayValue);
                }
                grid.Rows.Add(kvp.Key, displayValue);
            }
            RefreshRunningApps();
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                if (!row.IsNewRow)
                    grid.Rows.Remove(row);
            }
        }

        private void RefreshRunningApps()
        {
            runningAppsCombo.Items.Clear();
            var processes = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLower() + ".exe")
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            foreach (var name in processes)
            {
                runningAppsCombo.Items.Add(name);
            }
            if (runningAppsCombo.Items.Count > 0)
                runningAppsCombo.SelectedIndex = 0;
        }

        private void AddAppButton_Click(object? sender, EventArgs e)
        {
            var selectedApp = runningAppsCombo.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedApp))
            {
                // Ajoute l'application à la grille si elle n'existe pas déjà
                bool exists = false;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells[0].Value?.ToString()?.Equals(selectedApp, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    grid.Rows.Add(selectedApp, "");
                }
            }
        }

        private void OnForegroundWindowChanged(string exeName)
        {
            layoutSwitcher.CheckAndSwitch(exeName);
        }

        private void EnableSwitcherCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (enableSwitcherCheckBox.Checked)
            {
                if (winEventHook == null)
                {
                    winEventHook = new WinEventHook();
                    winEventHook.ForegroundWindowChanged += OnForegroundWindowChanged;
                }
            }
            else
            {
                winEventHook?.Dispose();
                winEventHook = null;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            winEventHook?.Dispose();
            base.OnFormClosed(e);
        }

        private void TestSwitchButton_Click(object? sender, EventArgs e)
        {
            KeyboardManager.SwitchLayout("00000409"); // QWERTY US
            // ou "0000040C" pour AZERTY FR
        }

        private void TestSwitchFrButton_Click(object? sender, EventArgs e)
        {
            KeyboardManager.SwitchLayout("0000040C"); // AZERTY FR
        }
    }
}