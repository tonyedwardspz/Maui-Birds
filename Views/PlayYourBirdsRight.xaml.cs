using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Helpers;
using Maui_Birds.Midi;
using Maui_Birds.Models;

namespace Maui_Birds.Views;

public partial class PlayYourBirdsRight : ContentPage
{
	private List<Bird> _birds;
	public List<Bird> Birds
	{
		get => _birds;
		set
		{
			_birds = value;
			OnPropertyChanged(nameof(Birds));
		}
	}
	public ObservableCollection<Bird> TeamABirds { get; set; } = new ObservableCollection<Bird>();
	public ObservableCollection<Bird> TeamBBirds { get; set; } = new ObservableCollection<Bird>();

	public PlayYourBirdsRight()
	{
		InitializeComponent();
		BindingContext = this;
		LoadBirdsAsync();
		InitializeMidiAsync();

	}

	private bool teamSelected = false;
	private bool HasSelected = false;

	private bool GuessedHigher = false;

	public string CurrentTeam { get; set; } = "A";

	private async Task LoadBirdsAsync()
	{
		Birds = await BirdHelper.LoadConfig("data.json");
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
		// Game start
		if (!teamSelected)
		{
			CurrentTeam = SelectStartingTeam(note);
			teamSelected = true;

			var random = new Random();
			var randomBird = Birds[random.Next(Birds.Count)];
			if (CurrentTeam == "A")
				TeamABirds.Add(randomBird);
			else
				TeamBBirds.Add(randomBird);
			UpdateGameBoard(randomBird, true);
			return;
		}

		// Bird selected
		if ((note >= 41 && note <= 72) && !HasSelected){
			Debug.WriteLine("Bird Selected");
			HasSelected = true;
		}

		// Higher guess
		if ((note >= 32 && note <= 39) && HasSelected){
			Debug.WriteLine("Higher Guess");
		}

		// Lower guess
		if ((note >= 0 && note <= 7) && HasSelected){
			Debug.WriteLine("Lower Guess");
		}

		// Reveal answer
		if (note == 93){
			Debug.WriteLine("Reveal Answer");
			HasSelected = false;
			CalculateAnswer();
		}



		var bird = Birds.FirstOrDefault(b => b.Id == note);
		UpdateGameBoard(bird);
		
		MainThread.BeginInvokeOnMainThread(() =>
		{
			BirdSongPlayer.Source = MediaSource.FromResource($"{bird.CommonName.Replace(" ", "_").ToLower()}.mp3");
			BirdSongPlayer.Play();
		});
	}

	private void HandleNoteOff(int note)
	{
		Debug.WriteLine($"Note off from main page: {note}");
		MainThread.BeginInvokeOnMainThread(() =>
		{
			BirdSongPlayer.Stop();
		});

		// // Bird selected, key released
		// if ((note >= 41 && note <= 72) && !HasSelected){

		// }
	}

	private void CalculateAnswer()
	{
        var birds = CurrentTeam == "A" ? TeamABirds : TeamBBirds;

		var lastBird = birds.Last();
		var previousBird = birds.ElementAt(birds.Count - 2);

		if (lastBird.Sightings > previousBird.Sightings){
			Debug.WriteLine("Correct Guess");
		}
		else if (lastBird.Sightings == previousBird.Sightings){
			Debug.WriteLine("Nothing for a pair");
		}
		else{
			Debug.WriteLine("Incorrect Guess");
		}

		int index = CurrentTeam == "A" ? TeamABirds.Count - 1 : TeamBBirds.Count - 1;
		string previousBirdBox = $"Team{CurrentTeam}Bird{index - 1}";
        MainThread.BeginInvokeOnMainThread(() => this.FindByName<Label>(previousBirdBox + "SightingsLabel").Text = lastBird.Sightings.ToString());
	}


	private void UpdateGameBoard(Bird? bird, bool GameStart = false)
	{
		if (bird != null)
		{
			int index = CurrentTeam == "A" ? TeamABirds.Count - 1 : TeamBBirds.Count - 1;
			string birdBox = $"Team{CurrentTeam}Bird{index}";
			string previousBirdBox = $"Team{CurrentTeam}Bird{index - 1}";

			if (CurrentTeam == "A")
				TeamABirds.Add(bird);
			else
				TeamBBirds.Add(bird);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.FindByName<Image>(birdBox + "Image").Source = bird.ImageFilename;
				this.FindByName<Label>(birdBox + "NameLabel").Text = bird.CommonName;

				if (index <= 0)
					this.FindByName<Label>(birdBox + "SightingsLabel").Text = bird.Sightings.ToString();
			});
		}
	}


	private string SelectStartingTeam(int note)
    {
        // Define ranges for Team A
        int[][] teamARanges = new int[][]
        {
            new int[] {0, 3}, new int[] {8, 11},
            new int[] {16, 19}, new int[] {24, 27},
            new int[] {32, 35}
        };

        // Check if the note number falls into any of the Team A ranges
        foreach (var range in teamARanges)
        {
            if (note >= range[0] && note <= range[1])
            {
				Debug.WriteLine("Team A");
                return "A";
            }
        }

        Debug.WriteLine("Team B");
        return "B";
    }
}
