using System.Collections.ObjectModel;
using System.Diagnostics;
using Maui_Birds.Helpers;
using Maui_Birds.Models;

namespace Maui_Birds.Services;

public class BirdSearchService
{
    public List<Bird>? SearchBirds { get; set; } = [];
    private List<Bird>? AllBirds { get; set; }

    private static BirdSearchService? _instance;
    public static BirdSearchService Instance => _instance ??= new BirdSearchService();

    private BirdSearchService()
    {
        _ = LoadBirdsAsync();
    }
    
    private async Task LoadBirdsAsync()
    {
        AllBirds = await BirdHelper.LoadConfig("data.json");
    }

    // Optional: search birds by name
    public IEnumerable<Bird> Search(string query)
    {
        //return Birds.Where(b => b.CommonName.Contains(query, StringComparison.OrdinalIgnoreCase));
        
        SearchBirds = AllBirds
            .Where(bird => bird.CommonName.ToLower().Contains(query.ToLower()))
            .ToList<Bird>();
        
        Debug.WriteLine("Results length: " + SearchBirds.Count);

        return SearchBirds;
    }
}
