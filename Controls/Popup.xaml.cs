namespace Maui_Birds.Controls;

public partial class PopupPage 
{
	private string message;
	public string Message
	{
		get => message;
		set
		{
			message = value;
			OnPropertyChanged();
		}
	}

	public PopupPage(string message)
	{
		InitializeComponent();
		BindingContext = this;

		Message = message;
	}
}
