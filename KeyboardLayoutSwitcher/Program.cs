using System;

namespace KeyboardLayoutSwitcher
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new MainWindow();
            app.InitializeComponent();
            app.Run();
        }
    }
}