using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using WinRT;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Anti_Bunda_Mole.Platforms.Windows
{
    public static class OverlayManager
    {
        private static Microsoft.UI.Xaml.Window _overlayWindow;
        private static IntPtr _hwnd = IntPtr.Zero;
        private static MicaController _micaController;
        private static DesktopAcrylicController _acrylicController;
        private static SystemBackdropConfiguration _configuration;

        // Win32 constants
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

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const uint ABM_NEW = 0x00000000;
        private const uint ABM_REMOVE = 0x00000001;
        private const uint ABM_QUERYPOS = 0x00000002;
        private const uint ABM_SETPOS = 0x00000003;
        private const int ABE_LEFT = 0;
        private const int ABE_RIGHT = 2;

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
                return IntPtr.Zero; // ignora tentativas de fechar
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        public static void ShowOverlay(UIElement content, OverlaySide side, int width)
        {
            if (_overlayWindow != null) return;

            _overlayWindow = new Microsoft.UI.Xaml.Window();
            _overlayWindow.ExtendsContentIntoTitleBar = true;
            _overlayWindow.SetTitleBar(new Grid());

            var root = new Border
            {
                Padding = new Microsoft.UI.Xaml.Thickness(16),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Background = new SolidColorBrush(ColorHelper.FromArgb(180, 0, 0, 0)),
                Child = content
            };

            _overlayWindow.Content = root;
            _overlayWindow.Activate();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_overlayWindow);

            // Remove da taskbar e foco
            long exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle));

            // Remove botões, bordas e título
            long style = GetWindowLongPtr(_hwnd, GWL_STYLE).ToInt64();
            style &= ~(WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_THICKFRAME);
            SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(style));

            // Intercepta fechamento
            _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, _newWndProcDelegate);

            // Define posição como AppBar
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            RegisterAppBar(_hwnd, width, side, screenWidth, screenHeight);

            // Aplica efeito translúcido
            TrySetBackdrop(_overlayWindow);
        }

        private static void TrySetBackdrop(Microsoft.UI.Xaml.Window window)
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

        private static void RegisterAppBar(IntPtr hwnd, int width, OverlaySide side, int screenWidth, int screenHeight)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd,
                uEdge = (uint)(side == OverlaySide.Left ? ABE_LEFT : ABE_RIGHT),
                rc = new RECT { top = 0, bottom = screenHeight }
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

            SHAppBarMessage(ABM_NEW, ref abd);
            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            SetWindowPos(hwnd, IntPtr.Zero, abd.rc.left, abd.rc.top,
                abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, 0);
        }

        private static void RemoveAppBar(IntPtr hwnd)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd
            };
            SHAppBarMessage(ABM_REMOVE, ref abd);
        }
    }

    public enum OverlaySide
    {
        Left,
        Right
    }
}
