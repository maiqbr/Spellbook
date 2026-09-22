using System.Text.Json.Serialization;

namespace Spellbook;

public enum MacroEventType { KeyDown, KeyUp, MouseDown, MouseUp, MouseMove, MouseWheel }
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
    public int Repeat { get; set; } = 1;
    public List<MacroEvent> Events { get; set; } = [];
    [JsonIgnore] public int DurationMs => Events.Sum(x => x.DelayMs);
}
