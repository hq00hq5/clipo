using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Clipo
{
    public partial class App : Application
    {
        private NotifyIcon _notifyIcon;
        private MainWindow _mainWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Initialize main window, it will hide itself in Loaded event
            _mainWindow = new MainWindow();
            _mainWindow.Show(); // This is needed to ensure Window handles are created for hooks

            // Set up System Tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Clipo - Clipboard History"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open History", null, (s, args) => ShowMainWindow());
            contextMenu.Items.Add("Exit", null, (s, args) => ShutdownApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            _mainWindow.Visibility = Visibility.Visible;
            _mainWindow.Activate();
            _mainWindow.Focus();
        }

        private void ShutdownApplication()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _mainWindow.Close();
            Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}
