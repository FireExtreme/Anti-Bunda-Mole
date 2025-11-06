using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using WinRT;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Window = Microsoft.UI.Xaml.Window;

namespace Anti_Bunda_Mole.Platforms.Windows
{
    public static class OverlayManager
    {
        private static Window _overlayWindow;
        private static IntPtr _hwnd = IntPtr.Zero;
        private static MicaController _micaController;
        private static DesktopAcrylicController _acrylicController;
        private static SystemBackdropConfiguration _configuration;

        // Win32
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WS_EX_NOACTIVATE = 0x08000000;

        private const int WM_CLOSE = 0x0010;
        private const int GWL_WNDPROC = -4;
        private static IntPtr _oldWndProc = IntPtr.Zero;
        private static WndProcDelegate _newWndProcDelegate = CustomWndProc;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]

        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_CLOSE)
                return IntPtr.Zero; // ignora qualquer tentativa de fechar
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll", SetLastError = true)]
            private static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int X,
                int Y,
                int cx,
                int cy,
                uint uFlags);

        public static void ShowOverlay(UIElement content, OverlaySide side, int width)
        {
            if (_overlayWindow != null) return;

            _overlayWindow = new Window();
            _overlayWindow.ExtendsContentIntoTitleBar = true;
            _overlayWindow.SetTitleBar(new Grid());

            var root = new Border
            {
                Padding = new Microsoft.UI.Xaml.Thickness(16),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(ColorHelper.FromArgb(180, 0, 0, 0)),
                Child = content
            };

            _overlayWindow.Content = root;
            _overlayWindow.Activate();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_overlayWindow);

            // Remove da taskbar e foco
            long exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle));

            // Remove botões padrão
            long style = GetWindowLongPtr(_hwnd, GWL_STYLE).ToInt64();
            style &= ~(WS_CAPTION | WS_SYSMENU);
            SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(style));

            // Permite redimensionamento
            style |= WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
            SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(style));

            // Intercepta fechamento
            _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, _newWndProcDelegate);

            // Inicializa AppBar
            UpdateAppBarPosition(width, side);

            // Atualiza posição ao redimensionar
            _overlayWindow.SizeChanged += (s, e) =>
            {
                if (_hwnd == IntPtr.Zero) return;

                if (GetWindowRect(_hwnd, out RECT rect))
                {
                    int currentWidth = rect.right - rect.left;
                    UpdateAppBarPosition(currentWidth, side);
                }
            };

            TrySetBackdrop(_overlayWindow);
        }

        private static void UpdateAppBarPosition(int width, OverlaySide side)
        {
            if (_hwnd == IntPtr.Zero) return;

            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = _hwnd,
                uEdge = (uint)(side == OverlaySide.Left ? 0 : 2),
                rc = new RECT
                {
                    top = 0,
                    bottom = screenHeight
                }
            };

            if (side == OverlaySide.Left)
            {
                abd.rc.left = 0;
                abd.rc.right = width;
            }
            else
            {
                abd.rc.right = screenWidth;
                abd.rc.left = screenWidth - width;
            }

            SHAppBarMessage(0x00000000, ref abd);
            SHAppBarMessage(0x00000002, ref abd);
            SHAppBarMessage(0x00000003, ref abd);

            SetWindowPos(_hwnd, IntPtr.Zero, abd.rc.left, abd.rc.top,
                abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, 0);
        }

        private static void TrySetBackdrop(Window window)
        {
            _configuration = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            if (MicaController.IsSupported())
            {
                _micaController = new MicaController { Kind = MicaKind.BaseAlt };
                _micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configuration);
            }
            else
            {
                _acrylicController = new DesktopAcrylicController();
                _acrylicController.TintColor = ColorHelper.FromArgb(255, 20, 20, 20);
                _acrylicController.FallbackColor = ColorHelper.FromArgb(255, 30, 30, 30);
                _acrylicController.TintOpacity = 0.6f;
                _acrylicController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
                _acrylicController.SetSystemBackdropConfiguration(_configuration);
            }
        }

        public static void CloseOverlay()
        {
            if (_overlayWindow == null) return;

            RemoveAppBar(_hwnd);

            _micaController?.Dispose();
            _micaController = null;

            _acrylicController?.Dispose();
            _acrylicController = null;

            _configuration = null;

            _overlayWindow.Close();
            _overlayWindow = null;
            _hwnd = IntPtr.Zero;
        }

        private static void RemoveAppBar(IntPtr hwnd)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd
            };
            SHAppBarMessage(0x00000001, ref abd);
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }

    public enum OverlaySide
    {
        Left,
        Right
    }
}
