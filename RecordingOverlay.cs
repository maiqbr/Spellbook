using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Spellbook;

public sealed class RecordingOverlay : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExNoActivate = 0x08000000;
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;
    private readonly Border _banner;
    private readonly TextBlock _title;
    private readonly TextBlock _detail;
    private readonly Border _frame;

    public RecordingOverlay()
    {
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false;
        ShowActivated = false; Focusable = false; Topmost = true; ResizeMode = ResizeMode.NoResize;
        Left = SystemParameters.VirtualScreenLeft; Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth; Height = SystemParameters.VirtualScreenHeight;

        _frame = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(240, 200, 120)), BorderThickness = new Thickness(8), CornerRadius = new CornerRadius(10), Opacity = .92, IsHitTestVisible = false };
        _title = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(240, 200, 120)), FontFamily = new FontFamily("Georgia"), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center };
        _detail = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(241, 227, 196)), FontFamily = new FontFamily("Georgia"), FontSize = 15, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
        _banner = new Border { Background = new SolidColorBrush(Color.FromArgb(238, 32, 22, 9)), BorderBrush = new SolidColorBrush(Color.FromRgb(240, 200, 120)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(32, 18, 32, 18), IsHitTestVisible = false, Child = new StackPanel { Children = { _title, _detail } } };
        var root = new Grid { IsHitTestVisible = false }; root.Children.Add(_frame); root.Children.Add(_banner); Content = root;
        SourceInitialized += (_, _) => MakeClickThrough();
    }

    public void ShowCountdown(int seconds)
    {
        _frame.BorderBrush = new SolidColorBrush(Color.FromRgb(240, 200, 120));
        _banner.HorizontalAlignment = HorizontalAlignment.Center; _banner.VerticalAlignment = VerticalAlignment.Center;
        _title.Text = seconds.ToString(); _title.FontSize = 72;
        _detail.Text = "POSICIONE O CURSOR · A GRAVAÇÃO VAI COMEÇAR";
    }

    public void ShowRecording(string stopHotkey)
    {
        _frame.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 65, 76));
        _banner.HorizontalAlignment = HorizontalAlignment.Center; _banner.VerticalAlignment = VerticalAlignment.Top; _banner.Margin = new Thickness(0, 38, 0, 0);
        _title.Text = "●  GRAVANDO"; _title.FontSize = 19;
        _detail.Text = $"PRESSIONE {stopHotkey} PARA PARAR E VOLTAR AO SPELLBOOK";
    }

    private void MakeClickThrough()
    {
        var source = PresentationSource.FromVisual(this) as HwndSource;
        if (source is null) return;
        var style = NativeMethods.GetWindowLongPtr(source.Handle, GwlExStyle).ToInt64();
        NativeMethods.SetWindowLongPtr(source.Handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExNoActivate));
        source.AddHook((IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) => { if (message == WmNcHitTest) { handled = true; return new IntPtr(HtTransparent); } return IntPtr.Zero; });
    }
}
