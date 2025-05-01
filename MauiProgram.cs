using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Maui_Birds;

public static class MauiProgram
{
	
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()

            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement()

            .ConfigureFonts(fonts =>
			{	
				fonts.AddFont("Montserrat-Medium.ttf", "RegularFont");
				fonts.AddFont("Montserrat-SemiBold.ttf", "MediumFont");
				fonts.AddFont("Montserrat-Bold.ttf", "BoldFont");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

        return builder.Build();
	}
}
