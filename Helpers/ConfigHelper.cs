using System.Text.Json;
namespace Maui_Birds.Helpers;

public static class ConfigHelper
{
    public static async Task<Config> LoadConfig(string filename)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
        using var reader = new StreamReader(stream);

        var contents = reader.ReadToEnd();


        var config = JsonSerializer.Deserialize<Config>(contents);
        return config;
    }
}

public class Config
{
    public string? SyncFusionLicence { get; set; }
}
