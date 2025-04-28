using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maui_Birds.Services;

namespace Maui_Birds.Views;

public partial class BirdSearchView : ContentPage
{
    public BirdSearchService BirdService { get; } = BirdSearchService.Instance;
    
    public BirdSearchView()
    {
        InitializeComponent();
        BindingContext = this;
    }
}