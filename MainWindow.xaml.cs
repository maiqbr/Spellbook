using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Input;

namespace Spellbook;
public partial class MainWindow : Window
{
    private readonly MacroEngine _engine = new();
    private readonly SpellRepository _repository = new();
    private readonly ObservableCollection<Spell> _spells;
    private Spell? Current => SpellsList.SelectedItem as Spell;
    private bool _loading;
    private bool _capturingHotkey;
    private IntPtr _windowHandle;
    public MainWindow()
    {
        InitializeComponent();
        _spells = new(_repository.Load()); if (_spells.Count == 0) _spells.Add(new Spell { Name = "Primeiro feitiço" });
        SpellsList.ItemsSource = _spells; SpellsList.SelectedIndex = 0;
        _engine.EventRecorded += _ => Dispatcher.Invoke(RefreshEvents);
        _engine.RecordingChanged += active => Dispatcher.Invoke(() => { RecordButton.Content = active ? "■  PARAR GRAVAÇÃO" : "●  GRAVAR"; StatusText.Text = active ? "GRAVANDO" : "EM ESPERA"; StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(active ? "#F0C878" : "#6EE0A8")); });
        SourceInitialized += (_, _) => { _windowHandle = new WindowInteropHelper(this).Handle; HwndSource.FromHwnd(_windowHandle)?.AddHook(WindowMessage); RegisterAllHotkeys(); };
        Closed += (_, _) => { UnregisterAllHotkeys(); _engine.Dispose(); };
    }
    private void SpellsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Current is null) return; _loading = true; NameBox.Text = Current.Name; HotkeyBox.Text = Current.Hotkey; RepeatBox.Text = Current.Repeat.ToString(); TypeBox.SelectedIndex = (int)Current.Type;
        ClickButtonBox.SelectedIndex = (int)Current.AutoClicker.Button; ClickStyleBox.SelectedIndex = Math.Clamp(Current.AutoClicker.ClicksPerCycle, 1, 3) - 1; IntervalBox.Text = Current.AutoClicker.IntervalMs.ToString(); UnlimitedBox.IsChecked = Current.AutoClicker.Unlimited; FixedPositionBox.IsChecked = Current.AutoClicker.UseFixedPosition; AutoXBox.Text = Current.AutoClicker.X.ToString(); AutoYBox.Text = Current.AutoClicker.Y.ToString(); _loading = false; SetModePanels(); RefreshEvents();
    }
    private void RefreshEvents()
    {
        if (Current?.Type == SpellType.AutoClicker)
        {
            var c = Current.AutoClicker; EventsList.ItemsSource = new[] { $"{(c.Unlimited ? "∞" : Current.Repeat)} ciclos · {c.ClicksPerCycle} clique(s) · a cada {c.IntervalMs} ms", $"Botão: {c.Button} · Posição: {(c.UseFixedPosition ? $"{c.X}, {c.Y}" : "cursor atual")}" };
            DurationText.Text = c.Unlimited ? "∞" : $"{Math.Max(1, Current.Repeat) * c.IntervalMs / 1000d:0.0}s"; return;
        }
        var events = _engine.IsRecording ? _engine.Recording : Current?.Events ?? [];
        EventsList.ItemsSource = events.Select((x, i) => $"{i + 1:00}   +{x.DelayMs,4} ms     {Describe(x)}").ToList(); DurationText.Text = $"{events.Sum(x => x.DelayMs) / 1000d:0.0}s";
    }
    private static string Describe(MacroEvent x) => x.Type switch { MacroEventType.KeyDown or MacroEventType.KeyUp => $"{x.Type}: {(Key)x.Key}", MacroEventType.MouseWheel => $"Roda do mouse: {x.Data}", MacroEventType.Wait => "Esperar", _ => $"{x.Type}: {x.X}, {x.Y}" };
    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.IsRecording) { _engine.StopRecording(); if (Current is not null) { Current.Events = [.. _engine.Recording]; Persist(); RefreshEvents(); } }
        else _engine.StartRecording();
    }
    private async void PlayButton_Click(object sender, RoutedEventArgs e) { if (_engine.IsPlaying) { _engine.StopPlayback(); PlayButton.Content = "▷  TESTAR FEITIÇO"; return; } if (Current is not null) { StatusText.Text = "EXECUTANDO"; PlayButton.Content = "■  PARAR"; await _engine.PlayAsync(Current); StatusText.Text = "EM ESPERA"; PlayButton.Content = "▷  TESTAR FEITIÇO"; } }
    private void ClearButton_Click(object sender, RoutedEventArgs e) { if (Current is null) return; Current.Events.Clear(); Persist(); RefreshEvents(); }
    private void NewSpell_Click(object sender, RoutedEventArgs e) { var spell = new Spell { Name = $"Feitiço {_spells.Count + 1}" }; _spells.Add(spell); SpellsList.SelectedItem = spell; Persist(); }
    private void DeleteSpell_Click(object sender, RoutedEventArgs e)
    {
        if (Current is null) return;
        if (MessageBox.Show($"Excluir o feitiço “{Current.Name}”? Esta ação não pode ser desfeita.", "Excluir feitiço", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var index = SpellsList.SelectedIndex; _spells.Remove(Current); Persist();
        if (_spells.Count > 0) SpellsList.SelectedIndex = Math.Min(index, _spells.Count - 1);
        else { _loading = true; NameBox.Text = ""; HotkeyBox.Text = ""; RepeatBox.Text = "1"; TypeBox.SelectedIndex = -1; _loading = false; EventsList.ItemsSource = null; DurationText.Text = "0s"; }
    }
    private void NameBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_loading && Current is not null) { Current.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? "Feitiço sem nome" : NameBox.Text; SpellsList.Items.Refresh(); Persist(); } }
    private void DetailsChanged(object sender, TextChangedEventArgs e) { if (!_loading && Current is not null) { Current.Repeat = int.TryParse(RepeatBox.Text, out var repeat) ? Math.Clamp(repeat, 1, 99) : 1; Persist(); } }
    private void HotkeyBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Current is null) return;
        _capturingHotkey = true; HotkeyBox.Text = "PRESSIONE UMA TECLA..."; HotkeyBox.Focus(); e.Handled = true;
    }
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_capturingHotkey) return;
        e.Handled = true;
        if (Current is null) { _capturingHotkey = false; return; }
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        _capturingHotkey = false;
        if (key == Key.Escape) { HotkeyBox.Text = Current.Hotkey; return; }
        if (key is Key.Back or Key.Delete) { Current.Hotkey = ""; HotkeyBox.Text = ""; Persist(); return; }
        Current.Hotkey = FormatHotkey(Keyboard.Modifiers, key); HotkeyBox.Text = Current.Hotkey; Persist();
    }
    private static string FormatHotkey(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString()); return string.Join('+', parts);
    }
    private void Persist() { _repository.Save(_spells); RegisterAllHotkeys(); }
    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_loading && Current is not null) { Current.Type = (SpellType)Math.Max(0, TypeBox.SelectedIndex); Persist(); } SetModePanels(); RefreshEvents(); }
    private void SetModePanels()
    {
        var type = Current?.Type ?? SpellType.Recorded;
        AutoClickerPanel.Visibility = type == SpellType.AutoClicker ? Visibility.Visible : Visibility.Collapsed;
        ManualPanel.Visibility = type == SpellType.Manual ? Visibility.Visible : Visibility.Collapsed;
        RecordingPanel.Visibility = type == SpellType.Recorded ? Visibility.Visible : Visibility.Collapsed;
    }
    private void AutoClickerChanged(object sender, RoutedEventArgs e)
    {
        if (_loading || Current is null) return;
        var c = Current.AutoClicker; c.Button = (MouseButton)Math.Max(0, ClickButtonBox.SelectedIndex); c.ClicksPerCycle = Math.Max(1, ClickStyleBox.SelectedIndex + 1); c.IntervalMs = ReadInt(IntervalBox.Text, 100, 10, 60_000); c.Unlimited = UnlimitedBox.IsChecked == true; c.UseFixedPosition = FixedPositionBox.IsChecked == true; c.X = ReadInt(AutoXBox.Text, 0, 0, 100_000); c.Y = ReadInt(AutoYBox.Text, 0, 0, 100_000); Persist(); RefreshEvents();
    }
    private void AddManualAction_Click(object sender, RoutedEventArgs e)
    {
        if (Current is null) return;
        var delay = ReadInt(ManualDelayBox.Text, 100, 0, 60_000); var x = ReadInt(ManualXBox.Text, 0, 0, 100_000); var y = ReadInt(ManualYBox.Text, 0, 0, 100_000); var value = ManualValueBox.Text.Trim(); var type = ManualTypeBox.SelectedIndex;
        var events = Current.Events;
        if (type is 0 or 1) { if (!Enum.TryParse<Key>(value, true, out var key)) { MessageBox.Show("Informe uma tecla válida, por exemplo A, Enter ou F1.", "Spellbook"); return; } events.Add(new MacroEvent { Type = type == 0 ? MacroEventType.KeyDown : MacroEventType.KeyUp, Key = KeyInterop.VirtualKeyFromKey(key), DelayMs = delay }); }
        else if (type is 2 or 3) { var isRight = type == 3; events.Add(new MacroEvent { Type = MacroEventType.MouseDown, X = x, Y = y, Key = isRight ? NativeMethods.WM_RBUTTONDOWN : NativeMethods.WM_LBUTTONDOWN, DelayMs = delay }); events.Add(new MacroEvent { Type = MacroEventType.MouseUp, X = x, Y = y, Key = isRight ? NativeMethods.WM_RBUTTONUP : NativeMethods.WM_LBUTTONUP, DelayMs = 20 }); }
        else if (type == 4) events.Add(new MacroEvent { Type = MacroEventType.MouseMove, X = x, Y = y, DelayMs = delay });
        else if (type == 5) events.Add(new MacroEvent { Type = MacroEventType.MouseWheel, Data = ReadInt(value, 120, -12000, 12000), DelayMs = delay });
        else events.Add(new MacroEvent { Type = MacroEventType.Wait, DelayMs = delay });
        Persist(); RefreshEvents();
    }
    private static int ReadInt(string value, int fallback, int min, int max) => int.TryParse(value, out var parsed) ? Math.Clamp(parsed, min, max) : fallback;
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void RegisterAllHotkeys()
    {
        if (_windowHandle == IntPtr.Zero) return;
        UnregisterAllHotkeys();
        for (var i = 0; i < _spells.Count; i++) if (TryParseHotkey(_spells[i].Hotkey, out var modifiers, out var key)) NativeMethods.RegisterHotKey(_windowHandle, i + 1, modifiers, key);
    }
    private void UnregisterAllHotkeys() { if (_windowHandle != IntPtr.Zero) for (var i = 1; i <= _spells.Count; i++) NativeMethods.UnregisterHotKey(_windowHandle, i); }
    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_HOTKEY && wParam.ToInt32() is var id && id > 0 && id <= _spells.Count) { if (_engine.IsPlaying) _engine.StopPlayback(); else _ = _engine.PlayAsync(_spells[id - 1]); handled = true; }
        return IntPtr.Zero;
    }
    private static bool TryParseHotkey(string value, out uint modifiers, out uint key)
    {
        modifiers = 0; key = 0;
        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        foreach (var part in parts[..^1]) modifiers |= part.ToUpperInvariant() switch { "CTRL" or "CONTROL" => NativeMethods.MOD_CONTROL, "ALT" => NativeMethods.MOD_ALT, "SHIFT" => NativeMethods.MOD_SHIFT, "WIN" or "WINDOWS" => NativeMethods.MOD_WIN, _ => 0 };
        return Enum.TryParse<System.Windows.Input.Key>(parts[^1], true, out var parsed) && (key = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(parsed)) > 0;
    }
}
