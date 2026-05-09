using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipo.Models;
using Clipo.Services;
using System.Windows.Forms; // For SendKeys
using Clipboard = System.Windows.Clipboard; // Alias to avoid ambiguity
using System.IO;
using System.Windows.Media;

namespace Clipo
{
    public partial class MainWindow : Window
    {
        private DatabaseService _dbService;
        private ClipboardMonitor _clipboardMonitor;
        private HotkeyManager _hotkeyManager;
        private bool _isPasting;
        private System.Threading.Timer? _memTrimTimer;

        public MainWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            LoadWindowPosition();

            // Compatibility boost: software rendering fallback for older integrated GPUs
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        }

        private void LoadWindowPosition()
        {
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "window_bounds.txt");
                if (System.IO.File.Exists(configPath))
                {
                    var parts = System.IO.File.ReadAllText(configPath).Split(',');
                    if (parts.Length == 2 && double.TryParse(parts[0], out double left) && double.TryParse(parts[1], out double top))
                    {
                        this.WindowStartupLocation = WindowStartupLocation.Manual;
                        this.Left = left;
                        this.Top = top;
                    }
                }
            }
            catch { }
        }

        private void SaveWindowPosition()
        {
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "window_bounds.txt");
                System.IO.File.WriteAllText(configPath, $"{this.Left},{this.Top}");
            }
            catch { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _clipboardMonitor = new ClipboardMonitor(this);
            _clipboardMonitor.ClipboardChanged += OnClipboardChanged;

            _hotkeyManager = new HotkeyManager(this);
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

            // Hide window immediately after loading
            HideWindow();
        }

        private void OnClipboardChanged(object? sender, string text)
        {
            if (_isPasting) return; // Prevent loop

            _dbService.AddOrUpdateItem(text);
            if (Visibility == Visibility.Visible)
            {
                RefreshList();
            }
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
            }
        }

        private void ShowWindow()
        {
            SearchBox.Text = string.Empty;
            RefreshList();
            
            this.Visibility = Visibility.Visible;
            this.Topmost = true;
            this.Activate();
            this.Focus();
            SearchBox.Focus();
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

        private void HideWindow()
        {
            this.Visibility = Visibility.Hidden;
            SaveWindowPosition();

            // Defer memory trimming 5 seconds after idle to avoid interference with active tasks
            _memTrimTimer?.Dispose();
            _memTrimTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    SetProcessWorkingSetSize(
                        System.Diagnostics.Process.GetCurrentProcess().Handle,
                        UIntPtr.MaxValue, UIntPtr.MaxValue);
                }
                catch { }
            }, null, TimeSpan.FromSeconds(5), System.Threading.Timeout.InfiniteTimeSpan);
        }

        private int _searchSequence = 0;
        private async void RefreshList(string query = "")
        {
            int currentSequence = ++_searchSequence;
            var items = await Task.Run(() => _dbService.SearchItems(query));
            
            if (currentSequence == _searchSequence)
            {
                HistoryList.ItemsSource = items;
                if (items.Any())
                {
                    HistoryList.SelectedIndex = 0;
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshList(SearchBox.Text);
        }

        private DateTime _lastSpacePress = DateTime.MinValue;

        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space && string.IsNullOrEmpty(SearchBox.Text))
            {
                if ((DateTime.Now - _lastSpacePress).TotalMilliseconds < 300)
                {
                    HideWindow();
                    e.Handled = true;
                    return;
                }
                _lastSpacePress = DateTime.Now;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string text)
            {
                try
                {
                    Clipboard.SetText(text);
                    btn.Foreground = System.Windows.Media.Brushes.LimeGreen;
                    btn.Content = "✓";
                    await Task.Delay(500);
                    btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
                    btn.Content = "📋";
                }
                catch { }
            }
            e.Handled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // X button safely hides the window; app stays alive in system tray
            HideWindow();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Intercept any OS-level close attempt — hide instead of terminating
            e.Cancel = true;
            HideWindow();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideWindow();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (HistoryList.SelectedIndex < HistoryList.Items.Count - 1)
                {
                    HistoryList.SelectedIndex++;
                    HistoryList.ScrollIntoView(HistoryList.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (HistoryList.SelectedIndex > 0)
                {
                    HistoryList.SelectedIndex--;
                    HistoryList.ScrollIntoView(HistoryList.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                SelectItemAndPaste();
                e.Handled = true;
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SelectItemAndPaste(autoPaste: false);
                e.Handled = true;
            }
        }

        private async void SelectItemAndPaste(bool autoPaste = true)
        {
            if (HistoryList.SelectedItem is ClipboardItem item)
            {
                _isPasting = true;
                
                try
                {
                    Clipboard.SetText(item.Text);
                }
                catch { }

                HideWindow();

                if (autoPaste)
                {
                    // Delay slightly to ensure our window has yielded focus to the previous app
                    await Task.Delay(50);
                    SendKeys.SendWait("^v");
                }

                await Task.Delay(200);
                _isPasting = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _memTrimTimer?.Dispose();
            SaveWindowPosition();
            _clipboardMonitor?.Dispose();
            _hotkeyManager?.Dispose();
            base.OnClosed(e);
        }
    }
}