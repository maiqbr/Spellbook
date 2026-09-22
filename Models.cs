using System.Text.Json.Serialization;

namespace Spellbook;

public enum MacroEventType { KeyDown, KeyUp, MouseDown, MouseUp, MouseMove, MouseWheel, Wait }
public enum SpellType { Recorded, AutoClicker, Manual }
public enum MouseButton { Left, Right, Middle }
public sealed class MacroEvent
{
    public MacroEventType Type { get; set; }
    public int Key { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Data { get; set; }
    public int DelayMs { get; set; }
}
public sealed class Spell
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Novo feitiço";
    public string Hotkey { get; set; } = "";
    public SpellType Type { get; set; } = SpellType.Recorded;
    public int Repeat { get; set; } = 1;
    public AutoClickerSettings AutoClicker { get; set; } = new();
    public List<MacroEvent> Events { get; set; } = [];
    [JsonIgnore] public int DurationMs => Events.Sum(x => x.DelayMs);
}
public sealed class AutoClickerSettings
{
    public MouseButton Button { get; set; }
    public int IntervalMs { get; set; } = 100;
    public int ClicksPerCycle { get; set; } = 1;
    public bool Unlimited { get; set; }
    public bool UseFixedPosition { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
