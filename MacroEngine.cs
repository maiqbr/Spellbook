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
    private CancellationTokenSource? _playbackCts;
    public List<MacroEvent> Recording { get; } = [];
    public event Action<MacroEvent>? EventRecorded;
    public event Action<bool>? RecordingChanged;
    public bool IsRecording => _recording;
    public bool IsPlaying => _playing;

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
        if (_recording || _playing || (spell.Events.Count == 0 && spell.Type != SpellType.AutoClicker)) return;
        _playing = true;
        _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            if (spell.Type == SpellType.AutoClicker) { await PlayAutoClickerAsync(spell, _playbackCts.Token); return; }
            for (var cycle = 0; cycle < Math.Max(1, spell.Repeat); cycle++)
                foreach (var e in spell.Events)
                {
                    await Task.Delay(Math.Clamp(e.DelayMs, 0, 60_000), _playbackCts.Token);
                    Send(e);
                }
        }
        catch (OperationCanceledException) { }
        finally { _playbackCts?.Dispose(); _playbackCts = null; _playing = false; }
    }
    public void StopPlayback() => _playbackCts?.Cancel();
    private static async Task PlayAutoClickerAsync(Spell spell, CancellationToken cancellationToken)
    {
        var config = spell.AutoClicker;
        for (var i = 0; config.Unlimited || i < Math.Max(1, spell.Repeat); i++)
        {
            var point = new NativeMethods.POINT { x = config.X, y = config.Y };
            if (!config.UseFixedPosition) NativeMethods.GetCursorPos(out point);
            var (downKey, upKey) = config.Button switch { MouseButton.Right => (NativeMethods.WM_RBUTTONDOWN, NativeMethods.WM_RBUTTONUP), MouseButton.Middle => (0x0207, 0x0208), _ => (NativeMethods.WM_LBUTTONDOWN, NativeMethods.WM_LBUTTONUP) };
            for (var click = 0; click < Math.Clamp(config.ClicksPerCycle, 1, 3); click++) { Send(new MacroEvent { Type = MacroEventType.MouseDown, X = point.x, Y = point.y, Key = downKey }); Send(new MacroEvent { Type = MacroEventType.MouseUp, X = point.x, Y = point.y, Key = upKey }); }
            await Task.Delay(Math.Clamp(config.IntervalMs, 1, 60_000), cancellationToken);
        }
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
        if (e.Type == MacroEventType.Wait) return;
        NativeMethods.INPUT input;
        if (e.Type is MacroEventType.KeyDown or MacroEventType.KeyUp)
            input = new() { type = NativeMethods.INPUT_KEYBOARD, U = new() { ki = new() { wVk = (ushort)e.Key, dwFlags = e.Type == MacroEventType.KeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0 } } };
        else
        {
            var flags = e.Type switch
            {
                MacroEventType.MouseDown => e.Key == NativeMethods.WM_RBUTTONDOWN ? NativeMethods.MOUSEEVENTF_RIGHTDOWN : e.Key == 0x0207 ? NativeMethods.MOUSEEVENTF_MIDDLEDOWN : NativeMethods.MOUSEEVENTF_LEFTDOWN,
                MacroEventType.MouseUp => e.Key == NativeMethods.WM_RBUTTONUP ? NativeMethods.MOUSEEVENTF_RIGHTUP : e.Key == 0x0208 ? NativeMethods.MOUSEEVENTF_MIDDLEUP : NativeMethods.MOUSEEVENTF_LEFTUP,
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
