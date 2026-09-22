using System.Text.Json;
using System.IO;

namespace Spellbook;
public sealed class SpellRepository
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spellbook", "spells.json");
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spellbook", "settings.json");
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    public List<Spell> Load()
    {
        try { return File.Exists(_path) ? JsonSerializer.Deserialize<List<Spell>>(File.ReadAllText(_path), _options) ?? [] : []; }
        catch { return []; }
    }
    public void Save(IEnumerable<Spell> spells)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(spells, _options));
    }
    public SpellbookSettings LoadSettings()
    {
        try { return File.Exists(_settingsPath) ? JsonSerializer.Deserialize<SpellbookSettings>(File.ReadAllText(_settingsPath), _options) ?? new() : new(); }
        catch { return new(); }
    }
    public void SaveSettings(SpellbookSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, _options));
    }
}
