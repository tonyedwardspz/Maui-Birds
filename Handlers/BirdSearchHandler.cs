using System.Diagnostics;
using Maui_Birds.Models;

namespace Maui_Birds.Handlers;

public class BirdSearchHandler: SearchHandler
{
    public IList<Bird> Birds { get; set; }
    public Type SelectedItemNavigationTarget { get; set; }
    
    protected override void OnQueryChanged(string oldValue, string newValue)
    {
        base.OnQueryChanged(oldValue, newValue);
        
        Debug.WriteLine("Search Made");

        if (string.IsNullOrWhiteSpace(newValue))
        {
            ItemsSource = null;
        }
        else
        {
            ItemsSource = Birds
                .Where(bird => bird.CommonName.ToLower().Contains(newValue.ToLower()))
                .ToList<Bird>();
        }
    }

    protected override async void OnItemSelected(object item)
    {
        Debug.WriteLine("Search Item Select");
    }

    string GetNavigationTarget()
    {
        Debug.WriteLine("Search Item Navigated");
        
        return (Shell.Current as AppShell).Routes.FirstOrDefault(route => route.Value.Equals(SelectedItemNavigationTarget)).Key;
    }
}