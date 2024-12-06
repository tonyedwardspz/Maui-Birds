using System;
using System.Text.Json;
using Maui_Birds.Models;

namespace Maui_Birds.Helpers
{
	public class BirdHelper
	{
        public static async Task<List<Bird>> LoadConfig(string filename)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            using var reader = new StreamReader(stream);

            var contents = reader.ReadToEnd();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var config = JsonSerializer.Deserialize<List<Bird>>(contents, options);
            return config;
        }
    }
}

