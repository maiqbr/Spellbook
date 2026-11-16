using System.Runtime.InteropServices;

namespace Spellbook;
internal static class NativeMethods
{
    internal const uint INPUT_MOUSE = 0;
    internal const uint MOUSEEVENTF_MOVE = 0x0001, MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004, MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010, MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040;
    internal const uint ES_SYSTEM_REQUIRED = 0x00000001, ES_CONTINUOUS = 0x80000000;
    internal const int WM_HOTKEY = 0x0312;
    internal const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008;
    [DllImport("user32.dll")] internal static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("kernel32.dll")] internal static extern uint SetThreadExecutionState(uint flags);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    [StructLayout(LayoutKind.Sequential)] internal struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; }
    [StructLayout(LayoutKind.Sequential)] internal struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr extra; }
}
