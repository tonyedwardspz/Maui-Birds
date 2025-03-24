using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Controls;
using Maui_Birds.Helpers;
using Maui_Birds.Models;

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
	private ObservableCollection<Bird> TeamABirds { get; set; } = [];
	private ObservableCollection<Bird> TeamBBirds { get; set; } = [];
	private bool _teamSelected;
	private bool _hasSelected;
	private bool _guessedHigher;
	private string CurrentTeam { get; set; } = "A";
	
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
			await MidiManager.InitializeAsync();
			MidiManager.SetMIDICallback((status, data1, data2) =>
			{
				if ((status & 0xF0) == 0x90) // Note On message
				{
					HandleNoteOn(data1);
				}
				else
				{
					Debug.WriteLine($"Other MIDI message type: {status:X2}");
				}
			});
			SetupLights();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error initializing MIDI: {ex.Message}");
		}
	}

	private void HandleNoteOn(int note)
	{
		// Game start
		if (!_teamSelected)
		{
			CurrentTeam = SelectStartingTeam(note);
			_teamSelected = true;

			var random = new Random();
			if (Birds != null)
			{
				var randomBird = Birds[random.Next(Birds.Count)];
				UpdateGameBoard(randomBird);
			}

			return;
		}

		// Bird already selected and there's not a guess in progress
		if ((TeamABirds.Any(b => b.Id == note) || TeamBBirds.Any(b => b.Id == note)) && !_hasSelected){
			Debug.WriteLine("Bird Already Selected");
			MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage("This bird has already been selected")));
			return;
		}

		// Bird selected
		if (note is >= 41 and <= 72 && !_hasSelected){
			Debug.WriteLine("Bird Selected");
			_hasSelected = true;
		} else if (note is >= 41 and <= 72){
			Debug.WriteLine("Guess in progress");
			return;
		}

		// Higher guess
		if (note is >= 32 and <= 39){
			Debug.WriteLine("Higher Guess");
			_guessedHigher = true;
		}

		// Lower guess
		if (note is >= 0 and <= 7){ 
			Debug.WriteLine("Lower Guess");
			_guessedHigher = false;
		}

		// Reveal answer
		if (note == 93){
			Debug.WriteLine("Reveal Answer");
			CalculateAnswer();
		}

		if (note is >= 41 and <= 72){
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

        // Set up higher and lower lights in green
        var lightConfig = new Dictionary<byte, byte>();
        for (byte i = 0; i < 8; i++)  // Lower lights (0-7)
        {
            lightConfig[i] = (byte)5;
        }
        for (byte i = 32; i < 40; i++)  // Higher lights (32-39)
        {
            lightConfig[i] = (byte)1;
        }
        SetupLights(lightConfig);

        return team;
    }

	private void GameOver(string result)
    {
        // Stub: Implement logic to handle game over
		Debug.WriteLine("Game over: " + result);
		MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team {CurrentTeam} wins!")));
    }

	private void SetupLights(Dictionary<byte, byte>? lightConfig = null)
	{
		// First turn off all lights
		MidiManager.SetupLights();

		if (lightConfig != null)
		{
			MidiManager.SetupGameLights(lightConfig);
			return;
		}

		// Create a dictionary of light configurations
		var defaultConfig = new Dictionary<byte, byte>();
		
		byte count = 0;
		byte color = 1;
		for (byte i = 0; i < 40; i++)
		{
			if (count == 4)
			{
				count = 1;
				color = color == 1 ? (byte)5 : (byte)1;
			}
			else
			{
				count++;
			}
			defaultConfig[i] = color;
		}
		
		MidiManager.SetupGameLights(defaultConfig);
	}

	// when the page is disposed, remove the midi event handlers
	protected override void OnDisappearing()
	{
		try
        {
	        MidiManager.Cleanup();
            base.OnDisappearing();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up MIDI handlers: {ex.Message}");
        }
	}
}
