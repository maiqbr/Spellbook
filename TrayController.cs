using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Spellbook;

/// <summary>Owns the small tray-only experience and both background utilities.</summary>
public sealed class TrayController : IDisposable
{
    private readonly SpellRepository _repository = new();
    private readonly SpellbookSettings _settings;
    private readonly Forms.ContextMenuStrip _menu = new();
    private readonly Forms.NotifyIcon _tray;
    private readonly Forms.ToolStripMenuItem _autoToggle = new();
    private readonly Forms.ToolStripMenuItem _keepActiveToggle = new();
    private readonly Forms.ToolStripMenuItem _autoClickerMenu = new();
    private readonly Forms.ToolStripMenuItem _startWithWindows = new("Iniciar com o Windows") { CheckOnClick = true };
    private readonly Forms.ToolStripMenuItem _status = new() { Enabled = false };
    private readonly HotkeyWindow _hotkeyWindow = new();
    private CancellationTokenSource? _autoClickCancellation;
    private CancellationTokenSource? _keepActiveCancellation;

    public TrayController()
    {
        _settings = _repository.LoadSettings();
        _settings.ClickIntervalMs = Math.Clamp(_settings.ClickIntervalMs, 1, 60_000);
        _settings.KeepActiveIntervalSeconds = Math.Clamp(_settings.KeepActiveIntervalSeconds, 5, 86_400);
        if (!Enum.IsDefined(_settings.KeepActiveMode)) _settings.KeepActiveMode = KeepActiveMode.SilentMouseEvent;

        _menu.Items.Add(_status);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(BuildAutoClickerMenu());
        _menu.Items.Add(BuildHotkeyMenu());
        _menu.Items.Add(BuildKeepActiveMenu());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _startWithWindows.Click += (_, _) => SetStartWithWindows(_startWithWindows.Checked);
        _menu.Items.Add(_startWithWindows);
        _menu.Items.Add("Sair do Spellbook", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _tray = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Spellbook",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _hotkeyWindow.HotkeyPressed += (_, _) => ToggleAutoclicker();
        RegisterAutoclickerHotkey();
        RefreshMenu();
    }

    private Forms.ToolStripMenuItem BuildAutoClickerMenu()
    {
        var root = _autoClickerMenu;
        _autoToggle.Click += (_, _) => ToggleAutoclicker();
        root.DropDownItems.Add(_autoToggle);
        root.DropDownItems.Add(new Forms.ToolStripSeparator());

        var buttonMenu = new Forms.ToolStripMenuItem("Botão");
        foreach (var button in Enum.GetValues<MouseButton>())
        {
            var option = new Forms.ToolStripMenuItem(button switch { MouseButton.Left => "Esquerdo", MouseButton.Right => "Direito", _ => "Meio" }) { Tag = button };
            option.Click += (sender, _) => { _settings.ClickButton = (MouseButton)((Forms.ToolStripMenuItem)sender!).Tag!; SaveAndRefresh(); };
            buttonMenu.DropDownItems.Add(option);
        }

        var speedMenu = new Forms.ToolStripMenuItem("Intervalo entre cliques (1 ms a 60 s)");
        foreach (var milliseconds in new[] { 10, 25, 50, 100, 250, 500, 1000 })
        {
            var option = new Forms.ToolStripMenuItem($"{milliseconds} ms") { Tag = milliseconds };
            option.Click += (sender, _) => { _settings.ClickIntervalMs = (int)((Forms.ToolStripMenuItem)sender!).Tag!; SaveAndRefresh(); };
            speedMenu.DropDownItems.Add(option);
        }
        speedMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        var customInterval = new Forms.ToolStripTextBox { Text = _settings.ClickIntervalMs.ToString(), ToolTipText = "Intervalo personalizado em milissegundos" };
        speedMenu.DropDownItems.Add(customInterval);
        speedMenu.DropDownItems.Add("Aplicar intervalo personalizado", null, (_, _) => { if (int.TryParse(customInterval.Text, out var ms)) { _settings.ClickIntervalMs = Math.Clamp(ms, 1, 60_000); SaveAndRefresh(); } });

        root.DropDownItems.Add(buttonMenu);
        root.DropDownItems.Add(speedMenu);
        return root;
    }

    private Forms.ToolStripMenuItem BuildKeepActiveMenu()
    {
        var root = new Forms.ToolStripMenuItem("Manter ativo");
        _keepActiveToggle.Click += (_, _) => ToggleKeepActive();
        root.DropDownItems.Add(_keepActiveToggle);
        root.DropDownItems.Add(new Forms.ToolStripSeparator());

        var modeMenu = new Forms.ToolStripMenuItem("Modo");
        modeMenu.DropDownItems.Add(BuildKeepActiveModeOption("Impedir suspensão do Windows", KeepActiveMode.PreventSleep, "Mantém o sistema acordado sem criar atividade de mouse ou teclado."));
        modeMenu.DropDownItems.Add(BuildKeepActiveModeOption("Evento de mouse sem deslocamento", KeepActiveMode.SilentMouseEvent, "Envia atividade de mouse sem mover o cursor. Alguns aplicativos podem ignorá-la."));
        modeMenu.DropDownItems.Add(BuildKeepActiveModeOption("Micro movimento de compatibilidade", KeepActiveMode.MicroMovement, "Move o cursor um pixel e volta; útil somente quando os outros modos não forem suficientes."));

        var intervalMenu = new Forms.ToolStripMenuItem("Sinalizar atividade a cada");
        foreach (var seconds in new[] { 15, 30, 60, 120, 300, 600 })
        {
            var label = seconds < 60 ? $"{seconds} segundos" : $"{seconds / 60} minuto{(seconds == 60 ? "" : "s")}";
            var option = new Forms.ToolStripMenuItem(label) { Tag = seconds };
            option.Click += (sender, _) => { _settings.KeepActiveIntervalSeconds = (int)((Forms.ToolStripMenuItem)sender!).Tag!; SaveAndRefresh(); };
            intervalMenu.DropDownItems.Add(option);
        }
        intervalMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
        var customInterval = new Forms.ToolStripTextBox { Text = _settings.KeepActiveIntervalSeconds.ToString(), ToolTipText = "Intervalo personalizado em segundos" };
        intervalMenu.DropDownItems.Add(customInterval);
        intervalMenu.DropDownItems.Add("Aplicar intervalo personalizado", null, (_, _) => { if (int.TryParse(customInterval.Text, out var seconds)) { _settings.KeepActiveIntervalSeconds = Math.Clamp(seconds, 5, 86_400); SaveAndRefresh(); } });

        root.DropDownItems.Add(modeMenu);
        root.DropDownItems.Add(intervalMenu);
        return root;
    }

    private Forms.ToolStripMenuItem BuildHotkeyMenu()
    {
        var root = new Forms.ToolStripMenuItem("Atalho global do autoclicker");
        root.DropDownItems.Add(new Forms.ToolStripMenuItem("Use este atalho para iniciar ou parar os cliques.") { Enabled = false });
        var capture = new Forms.ToolStripTextBox { Text = FormatHotkey(), ReadOnly = true, ToolTipText = "Clique aqui e pressione a combinação desejada" };
        capture.KeyDown += (_, e) => CaptureAutoclickerHotkey(capture, e);
        root.DropDownItems.Add(capture);
        root.DropDownItems.Add("Remover atalho", null, (_, _) =>
        {
            _settings.AutoClickHotkeyVirtualKey = 0; _settings.AutoClickHotkeyModifiers = 0;
            _repository.SaveSettings(_settings); RegisterAutoclickerHotkey(); capture.Text = "Sem atalho";
            KeepMenuOpen();
        });
        return root;
    }

    private Forms.ToolStripMenuItem BuildKeepActiveModeOption(string text, KeepActiveMode mode, string tooltip)
    {
        var option = new Forms.ToolStripMenuItem(text) { Tag = mode, ToolTipText = tooltip };
        option.Click += (_, _) => SetKeepActiveMode(mode);
        return option;
    }

    private void ToggleAutoclicker()
    {
        if (_autoClickCancellation is not null) { _autoClickCancellation.Cancel(); _autoClickCancellation = null; }
        else
        {
            _autoClickCancellation = new CancellationTokenSource();
            _ = RunAutoclickerAsync(_autoClickCancellation);
        }
        RefreshMenu();
        KeepMenuOpen();
    }

    private void CaptureAutoclickerHotkey(Forms.ToolStripTextBox capture, Forms.KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        if (e.KeyCode is Forms.Keys.ControlKey or Forms.Keys.ShiftKey or Forms.Keys.Menu or Forms.Keys.LWin or Forms.Keys.RWin) return;
        if (e.KeyCode is Forms.Keys.Back or Forms.Keys.Delete)
        {
            _settings.AutoClickHotkeyVirtualKey = 0; _settings.AutoClickHotkeyModifiers = 0;
        }
        else
        {
            _settings.AutoClickHotkeyVirtualKey = (int)e.KeyCode;
            _settings.AutoClickHotkeyModifiers = ToNativeModifiers(e.Modifiers);
        }
        _repository.SaveSettings(_settings); RegisterAutoclickerHotkey(); capture.Text = FormatHotkey();
        KeepMenuOpen();
    }

    private void RegisterAutoclickerHotkey()
    {
        NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, 1);
        if (_settings.AutoClickHotkeyVirtualKey > 0)
            NativeMethods.RegisterHotKey(_hotkeyWindow.Handle, 1, _settings.AutoClickHotkeyModifiers, (uint)_settings.AutoClickHotkeyVirtualKey);
    }

    private async Task RunAutoclickerAsync(CancellationTokenSource source)
    {
        try
        {
            while (!source.IsCancellationRequested)
            {
                Click(_settings.ClickButton);
                await Task.Delay(_settings.ClickIntervalMs, source.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_autoClickCancellation, source)) _autoClickCancellation = null;
            RefreshMenu();
        }
    }

    private void ToggleKeepActive()
    {
        if (_keepActiveCancellation is not null)
        {
            if (_settings.KeepActiveMode == KeepActiveMode.PreventSleep)
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            _keepActiveCancellation.Cancel(); _keepActiveCancellation = null;
        }
        else
        {
            _keepActiveCancellation = new CancellationTokenSource();
            _ = RunKeepActiveAsync(_keepActiveCancellation);
        }
        RefreshMenu();
        KeepMenuOpen();
    }

    private async Task RunKeepActiveAsync(CancellationTokenSource source)
    {
        var mode = _settings.KeepActiveMode;
        try
        {
            if (mode == KeepActiveMode.PreventSleep)
            {
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
                await Task.Delay(Timeout.Infinite, source.Token);
                return;
            }

            while (!source.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.KeepActiveIntervalSeconds), source.Token);
                if (mode == KeepActiveMode.SilentMouseEvent) SendMouse(NativeMethods.MOUSEEVENTF_MOVE);
                else MoveMouseByOnePixelAndBack();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_keepActiveCancellation, source))
            {
                if (mode == KeepActiveMode.PreventSleep) NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
                _keepActiveCancellation = null;
            }
            RefreshMenu();
        }
    }

    private void SetKeepActiveMode(KeepActiveMode mode)
    {
        if (_settings.KeepActiveMode == mode) return;
        var wasActive = _keepActiveCancellation is not null;
        if (wasActive) ToggleKeepActive();
        _settings.KeepActiveMode = mode;
        _repository.SaveSettings(_settings);
        if (wasActive) ToggleKeepActive();
        RefreshMenu();
        KeepMenuOpen();
    }

    private static void Click(MouseButton button)
    {
        var (down, up) = button switch
        {
            MouseButton.Right => (NativeMethods.MOUSEEVENTF_RIGHTDOWN, NativeMethods.MOUSEEVENTF_RIGHTUP),
            MouseButton.Middle => (NativeMethods.MOUSEEVENTF_MIDDLEDOWN, NativeMethods.MOUSEEVENTF_MIDDLEUP),
            _ => (NativeMethods.MOUSEEVENTF_LEFTDOWN, NativeMethods.MOUSEEVENTF_LEFTUP)
        };
        SendMouse(down); SendMouse(up);
    }

    private static void MoveMouseByOnePixelAndBack()
    {
        SendMouse(NativeMethods.MOUSEEVENTF_MOVE, 1, 0);
        SendMouse(NativeMethods.MOUSEEVENTF_MOVE, -1, 0);
    }

    private static void SendMouse(uint flags, int dx = 0, int dy = 0)
    {
        var input = new NativeMethods.INPUT { type = NativeMethods.INPUT_MOUSE, U = new() { mi = new() { dx = dx, dy = dy, dwFlags = flags } } };
        NativeMethods.SendInput(1, [input], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private void SaveAndRefresh() { _repository.SaveSettings(_settings); RefreshMenu(); KeepMenuOpen(); }

    private void SetStartWithWindows(bool enabled)
    {
        const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var runKey = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
        if (runKey is null) return;
        if (enabled && Environment.ProcessPath is { } executablePath)
            runKey.SetValue("Spellbook", $"\"{executablePath}\"");
        else
            runKey.DeleteValue("Spellbook", throwOnMissingValue: false);

        _settings.StartWithWindows = enabled;
        _repository.SaveSettings(_settings);
        RefreshMenu();
        KeepMenuOpen();
    }

    private void RefreshMenu()
    {
        var autoActive = _autoClickCancellation is not null;
        var keepActive = _keepActiveCancellation is not null;
        _status.Text = $"Spellbook  ·  cliques {(autoActive ? "ativos" : "parados")} ({_settings.ClickIntervalMs} ms)  ·  manter ativo {(keepActive ? "ativo" : "parado")}";
        _autoClickerMenu.Text = $"Autoclicker infinito · {_settings.ClickIntervalMs} ms";
        _autoToggle.Text = autoActive ? $"■  Parar autoclicker ({_settings.ClickIntervalMs} ms)" : $"▶  Iniciar autoclicker infinito ({_settings.ClickIntervalMs} ms)";
        _keepActiveToggle.Text = keepActive ? "■  Desativar manter ativo" : $"▶  Ativar: {KeepActiveModeLabel()}";
        _startWithWindows.Checked = IsRegisteredForStartup();
        _tray.Text = $"Spellbook — cliques: {(autoActive ? "ON" : "OFF")} | ativo: {(keepActive ? "ON" : "OFF")}";
        RefreshChecks(_tray.ContextMenuStrip?.Items);
    }

    private void RefreshChecks(Forms.ToolStripItemCollection? items)
    {
        if (items is null) return;
        foreach (Forms.ToolStripItem item in items)
        {
            if (item is Forms.ToolStripMenuItem menu && menu.Tag is MouseButton button) menu.Checked = button == _settings.ClickButton;
            if (item is Forms.ToolStripMenuItem mode && mode.Tag is KeepActiveMode activeMode) mode.Checked = activeMode == _settings.KeepActiveMode;
            if (item is Forms.ToolStripMenuItem interval && interval.Tag is int value)
                interval.Checked = interval.OwnerItem?.Text == "Intervalo entre cliques"
                    ? value == _settings.ClickIntervalMs
                    : value == _settings.KeepActiveIntervalSeconds;
            if (item is Forms.ToolStripMenuItem nested) RefreshChecks(nested.DropDownItems);
        }
    }

    private string KeepActiveModeLabel() => _settings.KeepActiveMode switch
    {
        KeepActiveMode.PreventSleep => "impedir suspensão",
        KeepActiveMode.SilentMouseEvent => "evento silencioso",
        _ => "micro movimento"
    };

    private string FormatHotkey()
    {
        if (_settings.AutoClickHotkeyVirtualKey == 0) return "Sem atalho";
        var parts = new List<string>();
        if ((_settings.AutoClickHotkeyModifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((_settings.AutoClickHotkeyModifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((_settings.AutoClickHotkeyModifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((_settings.AutoClickHotkeyModifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(((Forms.Keys)_settings.AutoClickHotkeyVirtualKey).ToString());
        return string.Join('+', parts);
    }

    private static uint ToNativeModifiers(Forms.Keys modifiers) =>
        (modifiers.HasFlag(Forms.Keys.Control) ? NativeMethods.MOD_CONTROL : 0) |
        (modifiers.HasFlag(Forms.Keys.Alt) ? NativeMethods.MOD_ALT : 0) |
        (modifiers.HasFlag(Forms.Keys.Shift) ? NativeMethods.MOD_SHIFT : 0);

    private static bool IsRegisteredForStartup()
    {
        const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var runKey = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: false);
        return runKey?.GetValue("Spellbook") is string value && !string.IsNullOrWhiteSpace(value);
    }

    private void KeepMenuOpen()
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (!_menu.IsDisposed && !_menu.Visible) _menu.Show(Forms.Cursor.Position);
        });
    }

    private static Icon LoadIcon()
    {
        var path = Environment.ProcessPath;
        return path is not null ? Icon.ExtractAssociatedIcon(path) ?? SystemIcons.Application : SystemIcons.Application;
    }

    public void Dispose()
    {
        _autoClickCancellation?.Cancel(); _keepActiveCancellation?.Cancel();
        NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
        NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, 1); _hotkeyWindow.Dispose();
        _tray.Visible = false; _tray.Dispose();
    }

    private sealed class HotkeyWindow : Forms.NativeWindow, IDisposable
    {
        public event EventHandler? HotkeyPressed;

        public HotkeyWindow() => CreateHandle(new Forms.CreateParams());

        protected override void WndProc(ref Forms.Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == 1) HotkeyPressed?.Invoke(this, EventArgs.Empty);
            base.WndProc(ref m);
        }

        public void Dispose() => DestroyHandle();
    }
}
