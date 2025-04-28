using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Core.Extensions;
using Maui_Birds.Helpers;
using Maui_Birds.Models;

namespace Maui_Birds.Services;

public class BirdSearchService
{
    public ObservableCollection<Bird> SearchBirds { get; set; } = new ObservableCollection<Bird>();
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
    
    public void Search(string query)
    {
        SearchBirds.Clear();
        var results = AllBirds
            .Where(bird => bird.CommonName.ToLower().Contains(query.ToLower()))
            .ToList();
            
        foreach (var bird in results)
        {
            SearchBirds.Add(bird);
        }
        
        Debug.WriteLine("Results length: " + SearchBirds.Count);
    }
}
