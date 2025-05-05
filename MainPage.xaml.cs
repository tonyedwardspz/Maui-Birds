using System.Diagnostics;
using Maui_Birds.Helpers;
using Maui_Birds.Models;
using System.ComponentModel;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Services;

namespace Maui_Birds;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{

	public Bird CurrentBird { get; set; }
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
	private readonly BirdSearchService _birdSearchService;

	private List<Bird> _selectedBirds = new List<Bird>();
	public List<Bird> SelectedBirds 
	{ 
		get => _selectedBirds;
		set
		{
			_selectedBirds = value;
			OnPropertyChanged(nameof(SelectedBirds));
		}
	}

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;

		//Audience = new Audience(1);
		_birdSearchService = BirdSearchService.Instance;
		Birds = _birdSearchService.AllBirds;

		_ = InitializeMidiAsync();
	}

	private async Task InitializeMidiAsync()
	{
		// Set up the MIDI callback
		MidiManager.SetMIDICallback((status, data1, data2) =>
		{
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
		});

		// Initialize MIDI
		await MidiManager.InitializeAsync();
		MidiManager.SetupLights();
	}

	private void HandleNoteOn(int note, int velocity)
	{
		Debug.WriteLine($"Note from main page: {note}, Velocity: {velocity}");
		var bird = Birds.FirstOrDefault(b => b.Id == note);

		if (CurrentBird != null){

			SelectedBirds.Insert(0, CurrentBird);
			var tempBirds = SelectedBirds.ToList();
			SelectedBirds = new List<Bird>(tempBirds);
			OnPropertyChanged(nameof(SelectedBirds));

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

		CurrentBird = bird;

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

	protected override void OnDisappearing()
	{

		MidiManager.Cleanup();
		base.OnDisappearing();
	}
}
