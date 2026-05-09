using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Clipo.Services
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_SPACE = 0x20;

        private IntPtr _windowHandle;
        private HwndSource? _hwndSource;
        private int _hotkeyId = 9000;
        private bool _isDisposed;

        public event EventHandler? HotkeyPressed;

        public HotkeyManager(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(HwndHook);

            if (!RegisterHotKey(_windowHandle, _hotkeyId, MOD_SHIFT, VK_SPACE))
            {
                Console.WriteLine("Failed to register SHIFT+SPACE hotkey.");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                UnregisterHotKey(_windowHandle, _hotkeyId);
                _hwndSource?.RemoveHook(HwndHook);
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
