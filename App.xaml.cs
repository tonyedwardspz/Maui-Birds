using Maui_Birds.Helpers;

namespace Maui_Birds;

public partial class App : Application
{
    private static Config? Config { get; set; }

    public App()
	{
        Config = ConfigHelper.LoadConfig("appsettings.json").Result;
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Config.SyncFusionLicence);

        InitializeComponent();

		MainPage = new AppShell();
	}
}

