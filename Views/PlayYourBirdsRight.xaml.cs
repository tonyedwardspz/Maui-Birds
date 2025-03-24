using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Controls;
using Maui_Birds.Helpers;
// using Maui_Birds.Midi;
using Maui_Birds.Models;
using NewBindingMaciOS;
using Foundation;

namespace Maui_Birds.Views;

public partial class PlayYourBirdsRight : ContentPage
{
	private List<Bird>? _birds;
	public List<Bird>? Birds
	{
		get => _birds;
		set
		{
			_birds = value;
			OnPropertyChanged(nameof(Birds));
		}
	}
	private ObservableCollection<Bird> TeamABirds { get; set; } = new ObservableCollection<Bird>();
	private ObservableCollection<Bird> TeamBBirds { get; set; } = new ObservableCollection<Bird>();
	private bool _teamSelected = false;
	private bool _hasSelected = false;
	private bool _guessedHigher = false;
	private string CurrentTeam { get; set; } = "A";
	
	private MIDILightController? _midiController;
	
	public PlayYourBirdsRight()
	{
		InitializeComponent();
		BindingContext = this;
        _ = LoadBirdsAsync();
        _ = InitializeMidiAsync();
	}

	private async Task LoadBirdsAsync()
	{
		Birds = await BirdHelper.LoadConfig("data.json");
	}

	private async Task InitializeMidiAsync()
	{
		try
		{
			_midiController = new MIDILightController();
			NSError? error;
			
			if (!_midiController.InitializeAndReturnError(out error))
			{
				Debug.WriteLine($"Failed to initialize MIDI: {error?.Description}");
				return;
			}

			var availableSources = _midiController.GetAvailableSourcesWithContaining("APC Key 25");
			if (availableSources.Length > 0)
			{
				// Connect to the first matching source
				if (!_midiController.ConnectSourceAt(0, out error))
				{
					Debug.WriteLine($"Failed to connect to MIDI source: {error?.Description}");
					return;
				}

				// Set up the callback for MIDI messages
				_midiController.SetMIDIReceiveCallback((byte status, byte data1, byte data2) =>
				{
					if ((status & 0xF0) == 0x90) // Note On message
					{
						HandleNoteOn(data1, data2);
					}
					else
					{
						Debug.WriteLine($"Other MIDI message type: {status:X2}");
					}
				});
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error initializing MIDI: {ex.Message}");
		}
	}

	private void HandleNoteOn(int note, int velocity)
	{
		// Game start
		if (!_teamSelected)
		{
			CurrentTeam = SelectStartingTeam(note);
			_teamSelected = true;

			var random = new Random();
			var randomBird = Birds[random.Next(Birds.Count)];
			UpdateGameBoard(randomBird);
			return;
		}

		// Bird already selected and there's not a guess in progress
		if ((TeamABirds.Any(b => b.Id == note) || TeamBBirds.Any(b => b.Id == note)) && !_hasSelected){
			Debug.WriteLine("Bird Already Selected");
			MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage("This bird has already been selected")));
			return;
		}

		// Bird selected
		if ((note >= 41 && note <= 72) && !_hasSelected){
			Debug.WriteLine("Bird Selected");
			_hasSelected = true;
		} else if (note >= 41 && note <= 72){
			Debug.WriteLine("Guess in progress");
			return;
		}

		// Higher guess
		if ((note >= 32 && note <= 39)){
			Debug.WriteLine("Higher Guess");
			_guessedHigher = true;
		}

		// Lower guess
		if ((note >= 0 && note <= 7)){ 
			Debug.WriteLine("Lower Guess");
			_guessedHigher = false;
		}

		// Reveal answer
		if (note == 93){
			Debug.WriteLine("Reveal Answer");
			CalculateAnswer();
		}

		if ((note >= 41 && note <= 72)){
			var bird = Birds.FirstOrDefault(b => b.Id == note);
			UpdateGameBoard(bird);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				var filename = $"{bird.CommonName.Replace(" ", "_").ToLower()}.mp3";

				if (FileSystem.AppPackageFileExistsAsync(filename).Result)
				{
					var source = MediaSource.FromResource(filename);
					BirdSongPlayer.Source = source;
				}
				else
				{
					var source = MediaSource.FromResource("placeholder.wav");
					BirdSongPlayer.Source = source;
				}

				BirdSongPlayer.Play();
			});
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

	private void CalculateAnswer()
	{
        var birds = CurrentTeam == "A" ? TeamABirds : TeamBBirds;
		var currentBird = birds.Last();
		var lastBird = birds.ElementAt(birds.Count - 2);
		var answer = currentBird.Sightings > lastBird.Sightings;

        if (currentBird.Sightings == lastBird.Sightings)
        {
            Debug.WriteLine("Nothing for a pair");
            MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage("Nothing for a pair.")));
            SwapTeams();
        }
		else if (answer == _guessedHigher)
        {
            Debug.WriteLine("Correct Guess");
        }
        else
        {
			Debug.WriteLine("Incorrect Guess");
			_hasSelected = false;
			SwapTeams();
			return;
		}

		var index = CurrentTeam == "A" ? TeamABirds.Count - 1 : TeamBBirds.Count - 1;
		var previousBirdBox = $"Team{CurrentTeam}Bird{index}";
        MainThread.BeginInvokeOnMainThread(() => this.FindByName<Label>(previousBirdBox + "SightingsLabel").Text = currentBird.Sightings.ToString());
		_hasSelected = false;

		// Game over
		if (CurrentTeam == "A" && TeamABirds.Count == 5){
			GameOver("complete");
		} else if (CurrentTeam == "B" && TeamBBirds.Count == 5){
			GameOver("complete");
		}		
	}

	private void SwapTeams(){
		var index = CurrentTeam == "A" ? TeamABirds.Count - 1 : TeamBBirds.Count - 1;
		var previousBirdBox = $"Team{CurrentTeam}Bird{index}";

		// remove the last item from the current teams bird collection
		if (CurrentTeam == "A")
			TeamABirds.RemoveAt(index);
		else
			TeamBBirds.RemoveAt(index);

		MainThread.BeginInvokeOnMainThread(() =>
		{
			this.FindByName<Image>(previousBirdBox + "Image").Source = "";
			this.FindByName<Label>(previousBirdBox + "NameLabel").Text = "";
			this.FindByName<Label>(previousBirdBox + "SightingsLabel").Text = "";
		});

		CurrentTeam = CurrentTeam == "A" ? "B" : "A";

		// if the new team has no birds, add a random bird to it that hasn't already been selected
		if (CurrentTeam == "A" && TeamABirds.Count == 0){
			var randomBird = Birds.Where(b => !TeamABirds.Contains(b) && !TeamBBirds.Contains(b)).OrderBy(b => b.Id).First();
            UpdateGameBoard(randomBird);
        } else if (CurrentTeam == "B" && TeamBBirds.Count == 0){
			var randomBird = Birds.Where(b => !TeamABirds.Contains(b) && !TeamBBirds.Contains(b)).OrderBy(b => b.Id).First();
            UpdateGameBoard(randomBird);
        }

		var notCurrentTeam = CurrentTeam == "A" ? "B" : "A";

		MainThread.BeginInvokeOnMainThread(() =>
		{
			this.FindByName<Label>("Team" + CurrentTeam + "Label").TextDecorations = TextDecorations.Underline;
			this.FindByName<Label>("Team" + notCurrentTeam + "Label").TextDecorations = TextDecorations.None;
		});
	}

	private void UpdateGameBoard(Bird? bird)
	{
		if (bird != null)
		{
			var index = CurrentTeam == "A" ? TeamABirds.Count : TeamBBirds.Count;
			if (index < 0) index = 0;
			
			var birdBox = $"Team{CurrentTeam}Bird{index}";

			if (CurrentTeam == "A")
				TeamABirds.Add(bird);
			else
				TeamBBirds.Add(bird);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.FindByName<Image>(birdBox + "Image").Source = bird.ImageFilename;
				if (bird.CommonName != null) this.FindByName<Label>(birdBox + "NameLabel").Text = bird.CommonName;

				if (index <= 0)
					this.FindByName<Label>(birdBox + "SightingsLabel").Text = bird.Sightings.ToString() ?? string.Empty;
			});
		}
	}

	private string SelectStartingTeam(int note)
    {
        int[][] teamARanges = new int[][]
        {
            new int[] {0, 3}, new int[] {8, 11},
            new int[] {16, 19}, new int[] {24, 27},
            new int[] {32, 35}
        };

		var team = "C";

        foreach (var range in teamARanges)
        {
            if (note >= range[0] && note <= range[1]) team = "A";
        }

		if (team != "A")
			team = "B";

		var teamLabel = $"Team{team}Label";

		MainThread.BeginInvokeOnMainThread(() =>
		{
			this.FindByName<Label>(teamLabel).TextDecorations = TextDecorations.Underline;
		});

        Debug.WriteLine(team);
        return team;
    }

	private void GameOver(string result)
    {
        // Stub: Implement logic to handle game over
		Debug.WriteLine("Game over: " + result);
		MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team {CurrentTeam} wins!")));
    }

	// when the page is disposed, remove the midi event handlers
	protected override void OnDisappearing()
	{
		try
        {
	        _midiController?.Dispose();
            base.OnDisappearing();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up MIDI handlers: {ex.Message}");
        }
	}
}
