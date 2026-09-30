using System.Runtime.InteropServices;

namespace UiMcpServer;

internal static class NativeMethods
{
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
    public static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extraInfo);

    public static void ClickAt(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MouseEventLeftDown, 0, 0, 0, IntPtr.Zero);
        mouse_event(MouseEventLeftUp, 0, 0, 0, IntPtr.Zero);
    }
}
