using System.Runtime.InteropServices;

namespace UiMcpServer;

// The few Win32 calls that UI Automation does not cover: DPI awareness, window
// bounds, a window screenshot and a real mouse click.
internal static class NativeMethods
{
    // PrintWindow flag: capture what the DWM has composed for the window, DirectX
    // content included. Without it, PrintWindow asks the window to paint itself over
    // WM_PRINT, which WPF does not do; the image stays blank.
    public const uint PrintWindowRenderFullContent = 2;

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr windowHandle, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr windowHandle, IntPtr deviceContext, uint flags);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", EntryPoint = "mouse_event")]
    private static extern void MouseEvent(uint flags, int dx, int dy, uint data, IntPtr extraInfo);

    // Last resort of ui_click for elements that offer no UI Automation pattern. It
    // moves the real cursor, so the window has to be visible on screen.
    public static void ClickAt(int x, int y)
    {
        SetCursorPos(x, y);
        MouseEvent(MouseEventLeftDown, 0, 0, 0, IntPtr.Zero);
        MouseEvent(MouseEventLeftUp, 0, 0, 0, IntPtr.Zero);
    }
}
