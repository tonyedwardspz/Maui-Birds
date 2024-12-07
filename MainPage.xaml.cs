using System.Diagnostics;
using Maui_Birds.Helpers;
using Maui_Birds.Midi;
using Maui_Birds.Models;
using System.ComponentModel;
using CommunityToolkit.Maui.Views;

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

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;

	    LoadBirdsAsync();
		InitializeMidiAsync();
	}

	private async Task LoadBirdsAsync()
	{
		Birds = await BirdHelper.LoadConfig("data.json");
		Debug.WriteLine(Birds.Count);
	}

	private async Task InitializeMidiAsync()
	{
		var inputs = MidiManager.AvailableInputDevices;
		Debug.WriteLine(inputs.Count);
		await MidiManager.EnsureInputReady("APC Key 25");

		MidiManager.ActiveInputDevices["APC Key 25"].NoteOn += HandleNoteOn;
		MidiManager.ActiveInputDevices["APC Key 25"].NoteOff += HandleNoteOff;
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
				BirdSongPlayer.Source = MediaSource.FromResource($"{bird.CommonName.Replace(" ", "_").ToLower()}.mp3");
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
}
