using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maui_Birds.Helpers;
using Maui_Birds.Models;

namespace Maui_Birds;

public partial class MainWindow : Window
{
    public IList<Bird> Birds { get; set; }
    public IList<Bird> Results { get; set; }
    
    public MainWindow()
    {
        InitializeComponent();

        _ = LoadBirdsAsync();
    }
    
    private async Task LoadBirdsAsync()
    {
        Birds = await BirdHelper.LoadConfig("data.json");
        Debug.WriteLine(Birds.Count);
    }
    
    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        Debug.WriteLine("Search text changed: " + e.NewTextValue);

        Results = Birds
            .Where(bird => bird.CommonName.ToLower().Contains(e.NewTextValue.ToLower()))
            .ToList<Bird>();

        // searchResults.ItemsSource = Results;
        
        Debug.WriteLine("Results length: " + Results.Count);
    }
}