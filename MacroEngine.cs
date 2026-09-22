using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spellbook;

public sealed class MacroEngine : IDisposable
{
    private IntPtr _keyboardHook, _mouseHook;
    private readonly NativeMethods.HookProc _keyboardProc, _mouseProc;
    private readonly Stopwatch _clock = new();
    private long _lastEvent;
    private bool _recording, _playing;
    public List<MacroEvent> Recording { get; } = [];
    public event Action<MacroEvent>? EventRecorded;
    public event Action<bool>? RecordingChanged;
    public bool IsRecording => _recording;

    public MacroEngine()
    {
        _keyboardProc = KeyboardCallback; _mouseProc = MouseCallback;
        var module = NativeMethods.GetModuleHandle(null);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);
    }
    public void StartRecording()
    {
        if (_playing) return;
        Recording.Clear(); _clock.Restart(); _lastEvent = 0; _recording = true; RecordingChanged?.Invoke(true);
    }
    public void StopRecording()
    {
        _recording = false; _clock.Stop(); RecordingChanged?.Invoke(false);
    }
    public async Task PlayAsync(Spell spell, CancellationToken cancellationToken = default)
    {
        if (_recording || _playing || spell.Events.Count == 0) return;
        _playing = true;
        try
        {
            for (var cycle = 0; cycle < Math.Max(1, spell.Repeat); cycle++)
                foreach (var e in spell.Events)
                {
                    await Task.Delay(Math.Clamp(e.DelayMs, 0, 60_000), cancellationToken);
                    Send(e);
                }
        }
        finally { _playing = false; }
    }
    private void Add(MacroEvent e)
    {
        if (!_recording || _playing) return;
        var now = _clock.ElapsedMilliseconds;
        e.DelayMs = (int)Math.Clamp(now - _lastEvent, 0, 60_000); _lastEvent = now;
        Recording.Add(e); EventRecorded?.Invoke(e);
    }
    private IntPtr KeyboardCallback(int nCode, IntPtr w, IntPtr l)
    {
        if (nCode >= 0 && _recording)
        {
            var key = Marshal.ReadInt32(l);
            var type = w.ToInt32() is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN ? MacroEventType.KeyDown : MacroEventType.KeyUp;
            Add(new MacroEvent { Type = type, Key = key });
        }
        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, w, l);
    }
    private IntPtr MouseCallback(int nCode, IntPtr w, IntPtr l)
    {
        if (nCode >= 0 && _recording)
        {
            var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(l);
            var type = w.ToInt32() switch
            {
                NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_RBUTTONDOWN => MacroEventType.MouseDown,
                NativeMethods.WM_LBUTTONUP or NativeMethods.WM_RBUTTONUP => MacroEventType.MouseUp,
                NativeMethods.WM_MOUSEMOVE => MacroEventType.MouseMove,
                NativeMethods.WM_MOUSEWHEEL => MacroEventType.MouseWheel,
                _ => (MacroEventType?)null
            };
            if (type is not null) Add(new MacroEvent { Type = type.Value, X = data.pt.x, Y = data.pt.y, Data = unchecked((short)(data.mouseData >> 16)), Key = w.ToInt32() });
        }
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, w, l);
    }
    private static void Send(MacroEvent e)
    {
        NativeMethods.INPUT input;
        if (e.Type is MacroEventType.KeyDown or MacroEventType.KeyUp)
            input = new() { type = NativeMethods.INPUT_KEYBOARD, U = new() { ki = new() { wVk = (ushort)e.Key, dwFlags = e.Type == MacroEventType.KeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0 } } };
        else
        {
            var flags = e.Type switch
            {
                MacroEventType.MouseDown => e.Key == NativeMethods.WM_RBUTTONDOWN ? NativeMethods.MOUSEEVENTF_RIGHTDOWN : NativeMethods.MOUSEEVENTF_LEFTDOWN,
                MacroEventType.MouseUp => e.Key == NativeMethods.WM_RBUTTONUP ? NativeMethods.MOUSEEVENTF_RIGHTUP : NativeMethods.MOUSEEVENTF_LEFTUP,
                MacroEventType.MouseWheel => NativeMethods.MOUSEEVENTF_WHEEL,
                _ => NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE
            };
            var width = Math.Max(1, NativeMethods.GetSystemMetrics(0) - 1); var height = Math.Max(1, NativeMethods.GetSystemMetrics(1) - 1);
            input = new() { type = NativeMethods.INPUT_MOUSE, U = new() { mi = new() { dx = e.Type == MacroEventType.MouseWheel ? 0 : e.X * 65535 / width, dy = e.Type == MacroEventType.MouseWheel ? 0 : e.Y * 65535 / height, mouseData = unchecked((uint)e.Data), dwFlags = flags } } };
        }
        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());
    }
    public void Dispose() { if (_keyboardHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_keyboardHook); if (_mouseHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_mouseHook); }
}
