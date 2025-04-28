using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Models;
using Maui_Birds.Services;
using Microsoft.Maui.Controls;

namespace Maui_Birds.Views;

public partial class BirdSearchView : ContentPage
{
    public BirdSearchService BirdService { get; } = BirdSearchService.Instance;
    
    public BirdSearchView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private void OnBirdSelected(object sender, SelectionChangedEventArgs? e)
    {
        if (e == null) return;
        
        var bird = e.CurrentSelection.FirstOrDefault() as Bird;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var filename = $"{bird.CommonName.Replace(" ", "_").ToLower()}.mp3";

            if (FileSystem.AppPackageFileExistsAsync(filename).Result)
            {
                var source = MediaSource.FromResource(filename);
                AudioPlayer.Source = source;
            }
            else
            {
                var source = MediaSource.FromResource("placeholder.wav");
                AudioPlayer.Source = source;
            }

            AudioPlayer.Play();
        });
    }
}