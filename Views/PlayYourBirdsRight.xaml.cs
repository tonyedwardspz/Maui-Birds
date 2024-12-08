using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Controls;
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
	private bool teamSelected = false;
	private bool HasSelected = false;
	private bool GuessedHigher = false;
	public string CurrentTeam { get; set; } = "A";

	public PlayYourBirdsRight()
	{
		InitializeComponent();
		BindingContext = this;
		LoadBirdsAsync();
		InitializeMidiAsync();

	}

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
			UpdateGameBoard(randomBird);
			return;
		}

		// Bird already selected and theres not a guess in progress
		if ((TeamABirds.Any(b => b.Id == note) || TeamBBirds.Any(b => b.Id == note)) && !HasSelected){
			Debug.WriteLine("Bird Already Selected");
			MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage("This bird has already been selected")));
			return;
		}

		// Bird selected
		if ((note >= 41 && note <= 72) && !HasSelected){
			Debug.WriteLine("Bird Selected");
		} else if (note >= 41 && note <= 72){
			Debug.WriteLine("Guess in progress");
			return;
		}

		// Higher guess
		if ((note >= 32 && note <= 39)){
			Debug.WriteLine("Higher Guess");
			GuessedHigher = true;
		}

		// Lower guess
		if ((note >= 0 && note <= 7)){ 
			Debug.WriteLine("Lower Guess");
			GuessedHigher = false;
		}

		// Reveal answer
		if (note == 93){
			Debug.WriteLine("Reveal Answer");
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
		else if (answer == GuessedHigher)
        {
            Debug.WriteLine("Correct Guess");
        }
        else
        {
			Debug.WriteLine("Incorrect Guess");
			SwapTeams();
			return;
		}

		int index = CurrentTeam == "A" ? TeamABirds.Count - 1 : TeamBBirds.Count - 1;
		string previousBirdBox = $"Team{CurrentTeam}Bird{index}";
        MainThread.BeginInvokeOnMainThread(() => this.FindByName<Label>(previousBirdBox + "SightingsLabel").Text = currentBird.Sightings.ToString());
		HasSelected = false;
	}

	private void SwapTeams(){
		int index = CurrentTeam == "A" ? TeamABirds.Count - 1 : TeamBBirds.Count - 1;
		string previousBirdBox = $"Team{CurrentTeam}Bird{index}";

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
	}

	private void UpdateGameBoard(Bird? bird)
	{
		if (bird != null)
		{
			int index = CurrentTeam == "A" ? TeamABirds.Count : TeamBBirds.Count;
			if (index < 0) index = 0;
			
			string birdBox = $"Team{CurrentTeam}Bird{index}";

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
        int[][] teamARanges = new int[][]
        {
            new int[] {0, 3}, new int[] {8, 11},
            new int[] {16, 19}, new int[] {24, 27},
            new int[] {32, 35}
        };

        foreach (var range in teamARanges)
        {
            if (note >= range[0] && note <= range[1]) return "A";
        }

        Debug.WriteLine("Team B");
        return "B";
    }
}
