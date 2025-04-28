using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maui_Birds.Helpers;
using Maui_Birds.Models;
using Maui_Birds.Services;
using Maui_Birds.Views;

namespace Maui_Birds;

public partial class MainWindow : Window
{
    public IList<Bird> Birds { get; set; }
    public IList<Bird> Results { get; set; }
    private readonly BirdSearchService _birdSearchService;
    
    public MainWindow()
    {
        InitializeComponent();
        _birdSearchService = BirdSearchService.Instance;
        _ = LoadBirdsAsync();
    }
    
    private async Task LoadBirdsAsync()
    {
        Birds = await BirdHelper.LoadConfig("data.json");
        Debug.WriteLine(Birds.Count);
    }
    
    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        Debug.WriteLine("Search text changed: " + e.NewTextValue);
        
        if (!string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _birdSearchService.Search(e.NewTextValue);
            
            if (Shell.Current.CurrentPage is not BirdSearchView)
            {
                await Shell.Current.GoToAsync("BirdSearchView", false);
            }
        }
    }
}