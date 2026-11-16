namespace Spellbook;

public enum MouseButton { Left, Right, Middle }
public enum KeepActiveMode { PreventSleep, SilentMouseEvent, MicroMovement }
public sealed class SpellbookSettings
{
    public MouseButton ClickButton { get; set; } = MouseButton.Left;
    public int ClickIntervalMs { get; set; } = 50;
    public int KeepActiveIntervalSeconds { get; set; } = 60;
    public KeepActiveMode KeepActiveMode { get; set; } = KeepActiveMode.SilentMouseEvent;
    public bool StartWithWindows { get; set; }
    public int AutoClickHotkeyVirtualKey { get; set; } = 0x75; // F6
    public uint AutoClickHotkeyModifiers { get; set; }
}
