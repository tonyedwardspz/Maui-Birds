using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Controls;
using Maui_Birds.Helpers;
// using Maui_Birds.Midi;
using Maui_Birds.Models;
using FileSystem = Microsoft.Maui.Storage.FileSystem;
using NewBindingMaciOS;
using Foundation;

namespace Maui_Birds.Views;

public partial class FlockFortunes : ContentPage
{
	private List<Bird>? _birds;
	public List<Bird>? Birds
	{
		get => _birds;
		set => _birds = value;
	}

	private List<Bird>? _guessedBirds;
	public List<Bird>? GuessedBirds
	{
		get => _guessedBirds;
		set => _guessedBirds = value;
	}

	private bool hasGameStarted = false;
	private bool teamSelected = false;
	public string CurrentTeam { get; set; } = "A";

	private List<int> teamOneBirdButtons = new List<int> { 32, 24, 16, 8, 0 };
    private List<int> teamTwoBirdButtons = new List<int> { 39, 31, 23, 15, 7 };

    private int guessNumber = 0;

	public int teamAScore = 0;
	public int teamBScore = 0;
	public int teamAWrong = 0;
	public int teamBWrong = 0;

	private MIDILightController? midiController;

	public FlockFortunes()
	{
		InitializeComponent();
		BindingContext = this;

		GuessedBirds = new List<Bird>();

        _ = LoadBirdsAsync();
        _ = InitializeMidiAsync();
	}

	private async Task LoadBirdsAsync()
	{
		Birds = await BirdHelper.LoadConfig("data.json");
		Birds = Birds.OrderByDescending(b => b.Sightings).ToList().Take(5).ToList();
	}

	private async Task InitializeMidiAsync()
	{
		try
		{
			midiController = new MIDILightController();
			NSError? error;
			
			if (!midiController.InitializeAndReturnError(out error))
			{
				Debug.WriteLine($"Failed to initialize MIDI: {error?.Description}");
				return;
			}

			var availableSources = midiController.GetAvailableSourcesWithContaining("APC Key 25");
			if (availableSources.Length > 0)
			{
				// Connect to the first matching source
				if (!midiController.ConnectSourceAt(0, out error))
				{
					Debug.WriteLine($"Failed to connect to MIDI source: {error?.Description}");
					return;
				}

				// Set up the callback for MIDI messages
				midiController.SetMIDIReceiveCallback((byte status, byte data1, byte data2) =>
				{
					if ((status & 0xF0) == 0x90) // Note On message
					{
						HandleNoteOn(data1, data2);
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
		Debug.WriteLine($"Note on from Flock game: {note}");
		
		// Listen for the starting team key 48 / 72
		if (!hasGameStarted && !teamSelected)
		{
			PlaySoundEffect("team_buzzer");
			if (note == 48){
				Debug.WriteLine("Team A hit first");
				SetCurrentTeam("B");
			} else if (note == 72){
				Debug.WriteLine("Team B hit first");
				SetCurrentTeam("A");
			}
			hasGameStarted = true;
			return;
		}

		// Listen for the first guess to decide in play team
		if (hasGameStarted && !teamSelected){
			if (teamOneBirdButtons.Contains(note) && CurrentTeam == "A")
			{
				CheckAnswer(teamOneBirdButtons.IndexOf(note), 1);
				SetCurrentTeam("B");
			}
			else if (teamTwoBirdButtons.Contains(note) && CurrentTeam == "B")
			{
				CheckAnswer(teamTwoBirdButtons.IndexOf(note), 2);
				SetCurrentTeam("A");
			}
			teamSelected = true;
			return;
		}

		// Listen for the wrong guesses to swap teams
		if (note == 34 || note == 35){
			teamAWrong++;
			WrongGuess("A", "TeamAWrong" + teamAWrong.ToString());
			return;
		} 
		else if (note == 36 || note == 37) {
			teamBWrong++;
			WrongGuess("B", "TeamBWrong" + teamBWrong.ToString());
			return;
		}

		// Listen for the correct guesses to update the score
		if (teamOneBirdButtons.Contains(note) && CurrentTeam == "A")
		{
			CheckAnswer(teamOneBirdButtons.IndexOf(note), 1);
		}
		else if (teamTwoBirdButtons.Contains(note) && CurrentTeam == "B")
		{
			CheckAnswer(teamTwoBirdButtons.IndexOf(note), 2);
		}
	}

	private void CheckAnswer(int guess, int team)
    {
		if (Birds == null) return;
		guessNumber++;
		var answer = Birds[guess];

		if (GuessedBirds.Contains(answer)) return;

		GuessedBirds.Add(answer);


		Debug.WriteLine($"Answer: {answer.CommonName}");
		UpdateTeamScore(team, answer.Sightings ?? 0);
		ShowAnswer(guess);
		PlaySoundEffect("correct");

		if (guessNumber == 5)
		{
			GameOver("complete");
		}
    }

	private void PlaySoundEffect(string effectName)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			string filename = $"{effectName}.mp3";

			if (FileSystem.AppPackageFileExistsAsync(filename).Result)
			{
				MediaSource source = MediaSource.FromResource(filename);
				AudioPlayer.Source = source;
			}
			AudioPlayer.Play();
		});
	}

	private void WrongGuess(string team, string imageName)
	{
		Debug.WriteLine("Wrong guess: " + team);
		Image wrongImage = this.FindByName<Image>(imageName);
		MainThread.BeginInvokeOnMainThread(() =>
		{
			wrongImage.Source = "wrong.png";
		});
		PlaySoundEffect("incorrect");

		if (team == "A" && teamAWrong == 3){
			GameOver("wrong");
		} else if (team == "B" && teamBWrong == 3){
			GameOver("wrong");
		} else {
			SetCurrentTeam(CurrentTeam);
		}
	}

	private void GameOver(string result)
    {
        // Stub: Implement logic to handle game over
		Debug.WriteLine("Game over: " + result);

		if (result == "complete"){
			if (teamAScore > teamBScore){
				MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team A wins with {teamAScore} points!")));
			} else {
				MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team B wins with {teamBScore} points!")));
			}
		} else if (result == "wrong"){
			MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team {CurrentTeam} loses :(")));
		}
    }
	
	private void SetCurrentTeam(string currentTeam)
	{
		CurrentTeam = currentTeam == "A" ? "B" : "A";
		Debug.WriteLine("Last team: " + currentTeam);
		Debug.WriteLine("Current team: " + CurrentTeam);

		Label activeTeamLabel = this.FindByName<Label>($"Team{CurrentTeam}Label");
		Label inactiveTeamLabel = this.FindByName<Label>($"Team{currentTeam}Label");

		MainThread.BeginInvokeOnMainThread(() =>
		{
			activeTeamLabel.TextDecorations = TextDecorations.Underline;
			activeTeamLabel.FontAttributes = FontAttributes.Bold;
			inactiveTeamLabel.TextDecorations = TextDecorations.None;
			inactiveTeamLabel.FontAttributes = FontAttributes.None;
		});
	}

    private void UpdateTeamScore(int team, int sightings)
    {
		Debug.WriteLine("Update team score: " + team + " " + sightings);
		if (team == 1){
			teamAScore += sightings;		
		} else {
			teamBScore += sightings;
		}
		MainThread.BeginInvokeOnMainThread(() =>
		{
			TeamAScore.Text = teamAScore.ToString();
			TeamBScore.Text = teamBScore.ToString();
		});
    }

    private void ShowAnswer(int index)
    {
		Debug.WriteLine("Show answer: " + index);
		Label AnswerLabel = this.FindByName<Label>($"Answer{index.ToString()}");
		Label AnswerSightings = this.FindByName<Label>($"Answer{index.ToString()}Sightings");
		
		if (Birds == null) return;
		MainThread.BeginInvokeOnMainThread(() =>
		{
			AnswerLabel.Text = Birds[index].CommonName;
			AnswerSightings.Text = Birds[index].Sightings.ToString();
		});
    }

	// when the page is disposed, remove the midi event handlers
	protected override void OnDisappearing()
	{
		midiController?.Dispose();
		base.OnDisappearing();
	}
}
