using Maui_Birds.Views;

namespace Maui_Birds;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		Routing.RegisterRoute(nameof(BirdSearchView), typeof(BirdSearchView));
	}
}

