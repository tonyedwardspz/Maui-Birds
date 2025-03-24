using System.Diagnostics;
using Maui_Birds.Helpers;
// using Maui_Birds.Midi;
using Maui_Birds.Models;
using System.ComponentModel;
using CommunityToolkit.Maui.Views;
using Foundation;
using NewBindingMaciOS;

namespace Maui_Birds;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private ImageSource _currentBirdImage;
	public ImageSource CurrentBirdImage
	{
		get => _currentBirdImage;
		set
		{
			_currentBirdImage = value;
			OnPropertyChanged(nameof(CurrentBirdImage));
		}
	}

	private string _currentBirdName;
	public string CurrentBirdName
	{
		get => _currentBirdName;
		set
		{
			_currentBirdName = value;
			OnPropertyChanged(nameof(CurrentBirdName));
		}
	}

	private string _imageCredit;
	public string ImageCredit
	{
		get => _imageCredit;
		set {
			_imageCredit = value;
			OnPropertyChanged(nameof(ImageCredit));
		}
	}

	private string _songCredit;
	public string SongCredit
	{
		get => _songCredit;
		set {
			_songCredit = value;
			OnPropertyChanged(nameof(SongCredit));
		}
	}

	private string _currentBirdSightings;
	public string CurrentBirdSightings
	{
		get => _currentBirdSightings;
		set {
			_currentBirdSightings = value;
			OnPropertyChanged(nameof(CurrentBirdSightings));
		}
	}

	private string _currentBirdSong;
	public string CurrentBirdSong
	{
		get => _currentBirdSong;
		set
		{
			_currentBirdSong = value;
			OnPropertyChanged(nameof(CurrentBirdSong));
		}
	}

	public List<Bird> Birds { get; set; }

	private List<ImageSource> _birdImages = new List<ImageSource>();
	public List<ImageSource> BirdImages 
	{ 
		get => _birdImages;
		set
		{
			_birdImages = value;
			OnPropertyChanged(nameof(BirdImages));
		}
	}

	public Audience Audience { get; set; }

	private MIDILightController _midiController;

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;

		Audience = new Audience(1);

        _ = LoadBirdsAsync();
        _ = InitializeMidiAsync();
	}

	private async Task LoadBirdsAsync()
	{
		Birds = await BirdHelper.LoadConfig("data.json");
		Debug.WriteLine(Birds.Count);
	}

	private async Task InitializeMidiAsync()
	{
		NSError? error;
		var midiController = new MIDILightController();
		Debug.WriteLine("Initializing MIDI controller...");
		var initialized = midiController.InitializeAndReturnError(out error);
		Debug.WriteLine($"MIDI controller initialized: {initialized}, Error: {error?.LocalizedDescription ?? "none"}");
		
		// Setup MIDI input callback
		Debug.WriteLine("Setting up MIDI callback...");
		midiController.SetMIDIReceiveCallback((byte status, byte data1, byte data2) =>
		{
			try
			{
				Debug.WriteLine("=== MIDI Callback Triggered ===");
				Debug.WriteLine($"Raw MIDI Message - Status: {status:X2}, Data1: {data1:X2}, Data2: {data2:X2}");
				
				MainThread.BeginInvokeOnMainThread(() =>
				{
					// Check if it's a Note On message (status byte: 0x90)
					if ((status & 0xF0) == 0x90)
					{
						Debug.WriteLine($"Note On detected - Note: {data1}, Velocity: {data2}");
						if (data2 > 0) // Some devices send Note On with velocity 0 for Note Off
						{
							HandleNoteOn(data1, data2);
						}
						else
						{
							HandleNoteOff(data1);
						}
					}
					// Check if it's a Note Off message (status byte: 0x80)
					else if ((status & 0xF0) == 0x80)
					{
						Debug.WriteLine($"Note Off detected - Note: {data1}");
						HandleNoteOff(data1);
					}
					else
					{
						Debug.WriteLine($"Other MIDI message type: {status:X2}");
					}
				});
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in MIDI callback: {ex.Message}");
				Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			}
		});

		// Connect to the first available MIDI source
		var sources = midiController.GetAvailableSourcesWithContaining("");
		Debug.WriteLine($"Available MIDI sources: {sources.Length}");
		foreach (var source in sources)
		{
			Debug.WriteLine($"Found MIDI source: {source}");
		}

		if (sources.Length > 0)
		{
			Debug.WriteLine("Attempting to connect to first MIDI source...");
			var connected = midiController.ConnectSourceAt(0, out error);
			Debug.WriteLine($"Connection result: {connected}, Error: {error?.LocalizedDescription ?? "none"}");
		}
		else
		{
			Debug.WriteLine("No MIDI sources found!");
		}

		SetupLights();
	}

	private void HandleNoteOn(int note, int velocity)
	{
		Debug.WriteLine($"Note from main page: {note}, Velocity: {velocity}");
		var bird = Birds.FirstOrDefault(b => b.Id == note);

		if (CurrentBirdImage != null){
			BirdImages.Add(CurrentBirdImage);
			var tempList = BirdImages.ToList();
			BirdImages = new List<ImageSource>(tempList);
			OnPropertyChanged(nameof(BirdImages));
		} else {
			try
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					BirdImageCreditLabel.IsVisible = true;
					BirdSongCreditLabel.IsVisible = true;
					BirdSightingsLabel.IsVisible = true;
				});
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error showing labels: {ex.Message}");
			}
		}

		try
		{
			CurrentBirdImage = $"{bird.CommonName.Replace(" ", "_").ToLower()}.jpg";
			CurrentBirdName = bird.CommonName;
			ImageCredit = bird.ImageCredit;
			SongCredit = bird.SongCredit;
			CurrentBirdSightings = bird.Sightings.ToString();
		} catch (Exception e)
		{

		}

		try
		{
            MainThread.BeginInvokeOnMainThread(() =>
            {
				string filename = $"{bird.CommonName.Replace(" ", "_").ToLower()}.mp3";
				var exists = FileSystem.AppPackageFileExistsAsync(filename).Result;
				
				if (exists)
				{
                    MediaSource source = MediaSource.FromResource(filename);
                    BirdSongPlayer.Source = source;
                } else
				{
					MediaSource source = MediaSource.FromResource("placeholder.wav");
                    BirdSongPlayer.Source = source;
                }
                BirdSongPlayer.Play();
            });
        }
		catch (Exception e)
		{
			Debug.WriteLine(e.Message);
		}
	}

	private void HandleNoteOff(int note)
	{
		Debug.WriteLine($"Note off from main page: {note}");
		MainThread.BeginInvokeOnMainThread(() =>
		{
			BirdSongPlayer.Stop();
		});
	}

	private void SetupLights()
	{
		NSError? error;
		var output = _midiController.GetAvailableDevicesWithContaining("Does not matter")[0];
		_midiController.ConnectTo(0, out error);
		
		// switch off all lights by looping over 0 to 39 in hex
		for (int i = 0; i < 40; i++){
			_midiController.TurnLightOnChannel((byte)0, (byte)i, (byte)0x00, out error);
		}
		
		
	}

	// when the page is disposed, remove the midi event handlers
	protected override void OnDisappearing()
	{
		try
		{
			if (_midiController != null)
			{
				// Clear the MIDI callback
				_midiController.SetMIDIReceiveCallback((_, _, _) => { });
				_midiController?.Dispose();
		base.OnDisappearing();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error cleaning up MIDI controller: {ex.Message}");
		}
	}
}
