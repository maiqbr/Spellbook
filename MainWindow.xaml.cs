using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;

namespace Spellbook;
public partial class MainWindow : Window
{
    private readonly MacroEngine _engine = new();
    private readonly SpellRepository _repository = new();
    private readonly ObservableCollection<Spell> _spells;
    private Spell? Current => SpellsList.SelectedItem as Spell;
    private bool _loading;
    private IntPtr _windowHandle;
    public MainWindow()
    {
        InitializeComponent();
        _spells = new(_repository.Load()); if (_spells.Count == 0) _spells.Add(new Spell { Name = "Primeiro feitiço" });
        SpellsList.ItemsSource = _spells; SpellsList.SelectedIndex = 0;
        _engine.EventRecorded += _ => Dispatcher.Invoke(RefreshEvents);
        _engine.RecordingChanged += active => Dispatcher.Invoke(() => { RecordButton.Content = active ? "■  PARAR GRAVAÇÃO" : "●  GRAVAR"; StatusText.Text = active ? "GRAVANDO" : "PRONTO"; StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(active ? "#F0C878" : "#6EE0A8")); });
        SourceInitialized += (_, _) => { _windowHandle = new WindowInteropHelper(this).Handle; HwndSource.FromHwnd(_windowHandle)?.AddHook(WindowMessage); RegisterAllHotkeys(); };
        Closed += (_, _) => { UnregisterAllHotkeys(); _engine.Dispose(); };
    }
    private void SpellsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Current is null) return; _loading = true; NameBox.Text = Current.Name; HotkeyBox.Text = Current.Hotkey; RepeatBox.Text = Current.Repeat.ToString(); _loading = false; RefreshEvents();
    }
    private void RefreshEvents()
    {
        var events = _engine.IsRecording ? _engine.Recording : Current?.Events ?? [];
        EventsList.ItemsSource = events.Select((x, i) => $"{i + 1:00}   +{x.DelayMs,4} ms     {Describe(x)}").ToList();
        DurationText.Text = $"{events.Sum(x => x.DelayMs) / 1000d:0.0}s";
    }
    private static string Describe(MacroEvent x) => x.Type switch { MacroEventType.KeyDown or MacroEventType.KeyUp => $"{x.Type}: {(System.Windows.Input.Key) x.Key}", MacroEventType.MouseWheel => $"Roda do mouse: {x.Data}", _ => $"{x.Type}: {x.X}, {x.Y}" };
    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.IsRecording) { _engine.StopRecording(); if (Current is not null) { Current.Events = [.. _engine.Recording]; Persist(); RefreshEvents(); } }
        else _engine.StartRecording();
    }
    private async void PlayButton_Click(object sender, RoutedEventArgs e) { if (Current is not null) { StatusText.Text = "CONJURANDO"; await _engine.PlayAsync(Current); StatusText.Text = "PRONTO"; } }
    private void ClearButton_Click(object sender, RoutedEventArgs e) { if (Current is null) return; Current.Events.Clear(); Persist(); RefreshEvents(); }
    private void NewSpell_Click(object sender, RoutedEventArgs e) { var spell = new Spell { Name = $"Feitiço {_spells.Count + 1}" }; _spells.Add(spell); SpellsList.SelectedItem = spell; Persist(); }
    private void NameBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_loading && Current is not null) { Current.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? "Feitiço sem nome" : NameBox.Text; SpellsList.Items.Refresh(); Persist(); } }
    private void DetailsChanged(object sender, TextChangedEventArgs e) { if (!_loading && Current is not null) { Current.Hotkey = HotkeyBox.Text; Current.Repeat = int.TryParse(RepeatBox.Text, out var repeat) ? Math.Clamp(repeat, 1, 99) : 1; Persist(); } }
    private void Persist() { _repository.Save(_spells); RegisterAllHotkeys(); SaveText.Text = "SALVO AGORA"; }
    private void RegisterAllHotkeys()
    {
        if (_windowHandle == IntPtr.Zero) return;
        UnregisterAllHotkeys();
        for (var i = 0; i < _spells.Count; i++) if (TryParseHotkey(_spells[i].Hotkey, out var modifiers, out var key)) NativeMethods.RegisterHotKey(_windowHandle, i + 1, modifiers, key);
    }
    private void UnregisterAllHotkeys() { if (_windowHandle != IntPtr.Zero) for (var i = 1; i <= _spells.Count; i++) NativeMethods.UnregisterHotKey(_windowHandle, i); }
    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_HOTKEY && wParam.ToInt32() is var id && id > 0 && id <= _spells.Count) { _ = _engine.PlayAsync(_spells[id - 1]); handled = true; }
        return IntPtr.Zero;
    }
    private static bool TryParseHotkey(string value, out uint modifiers, out uint key)
    {
        modifiers = 0; key = 0;
        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        foreach (var part in parts[..^1]) modifiers |= part.ToUpperInvariant() switch { "CTRL" or "CONTROL" => NativeMethods.MOD_CONTROL, "ALT" => NativeMethods.MOD_ALT, "SHIFT" => NativeMethods.MOD_SHIFT, "WIN" or "WINDOWS" => NativeMethods.MOD_WIN, _ => 0 };
        if (modifiers == 0) return false;
        return Enum.TryParse<System.Windows.Input.Key>(parts[^1], true, out var parsed) && (key = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(parsed)) > 0;
    }
}
