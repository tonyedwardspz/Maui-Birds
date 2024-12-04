using System.Diagnostics;
using Maui_Birds.Midi;

namespace Maui_Birds;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		InitializeMidiAsync();
	}

	private async Task InitializeMidiAsync()
	{
		var inputs = MidiManager.AvailableInputDevices;
		Debug.WriteLine(inputs.Count);
		await MidiManager.EnsureInputReady("APC Key 25");

		MidiManager.ActiveInputDevices["APC Key 25"].NoteOn += (note, velocity) =>
		{
			Debug.WriteLine($"Note from main page: {note}, Velocity: {velocity}");
		};
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}


