using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Clipboard = System.Windows.Clipboard;

namespace Clipo.Services
{
    public class ClipboardMonitor : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr _windowHandle;
        private HwndSource? _hwndSource;
        private bool _isDisposed;

        public event EventHandler<string>? ClipboardChanged;

        public ClipboardMonitor(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(HwndHook);
            AddClipboardFormatListener(_windowHandle);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                // Slightly delay to ensure the clipboard is unlocked by the source application
                Task.Delay(50).ContinueWith(_ =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (Clipboard.ContainsText())
                            {
                                string text = Clipboard.GetText();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    ClipboardChanged?.Invoke(this, text);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore exceptions if clipboard is locked
                        }
                    });
                });
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                RemoveClipboardFormatListener(_windowHandle);
                _hwndSource?.RemoveHook(HwndHook);
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
