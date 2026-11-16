namespace Spellbook;

public partial class App : System.Windows.Application
{
    private TrayController? _tray;

    private void App_Startup(object sender, System.Windows.StartupEventArgs e) => _tray = new TrayController();
    private void App_Exit(object sender, System.Windows.ExitEventArgs e) => _tray?.Dispose();
}
