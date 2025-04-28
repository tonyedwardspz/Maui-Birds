using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Controls;
using Maui_Birds.Helpers;
using Maui_Birds.Models;
using Maui_Birds.Services;
using FileSystem = Microsoft.Maui.Storage.FileSystem;


namespace Maui_Birds.Views;

public partial class FlockFortunes : ContentPage
{
	private List<Bird>? Birds { get; set; }
	private List<Bird>? GuessedBirds { get; init; }
	private readonly BirdSearchService _birdSearchService;

	private bool _hasGameStarted = false;
	private bool _teamSelected = false;
	private string CurrentTeam { get; set; } = "A";

	private readonly List<int> _teamOneBirdButtons = [32, 24, 16, 8, 0];
    private readonly List<int> _teamTwoBirdButtons = [39, 31, 23, 15, 7];

    private int _guessNumber = 0;
	private int _teamAScore = 0;
	private int _teamBScore = 0;
	private int _teamAWrong = 0;
	private int _teamBWrong = 0;

	public FlockFortunes()
	{
		InitializeComponent();
		BindingContext = this;

		_birdSearchService = BirdSearchService.Instance;

		GuessedBirds = [];

		Birds = _birdSearchService.AllBirds?.OrderByDescending(b => b.Sightings).ToList().Take(5).ToList();
        _ = InitializeMidiAsync();
	}

	private async Task InitializeMidiAsync()
	{
		try
		{
			await MidiManager.InitializeAsync();
			MidiManager.SetMIDICallback((byte status, byte data1, byte data2) =>
			{
				if ((status & 0xF0) == 0x90) // Note On message
				{
					HandleNoteOn(data1, data2);
				}
			});
			SetupLights();
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
		if (!_hasGameStarted && !_teamSelected)
		{
			PlaySoundEffect("team_buzzer");
			if (note == 48){
				Debug.WriteLine("Team A hit first");
				SetCurrentTeam("B");
			} else if (note == 72){
				Debug.WriteLine("Team B hit first");
				SetCurrentTeam("A");
			}
			_hasGameStarted = true;
			return;
		}

		// Listen for the first guess to decide in play team
		if (_hasGameStarted && !_teamSelected){
			if (_teamOneBirdButtons.Contains(note) && CurrentTeam == "A")
			{
				CheckAnswer(_teamOneBirdButtons.IndexOf(note), 1);
				SetCurrentTeam("B");
			}
			else if (_teamTwoBirdButtons.Contains(note) && CurrentTeam == "B")
			{
				CheckAnswer(_teamTwoBirdButtons.IndexOf(note), 2);
				SetCurrentTeam("A");
			}
			_teamSelected = true;
			return;
		}

		// Listen for the wrong guesses to swap teams
		if (note == 34 || note == 35){
			_teamAWrong++;
			WrongGuess("A", "TeamAWrong" + _teamAWrong.ToString());
			return;
		} 
		else if (note == 36 || note == 37) {
			_teamBWrong++;
			WrongGuess("B", "TeamBWrong" + _teamBWrong.ToString());
			return;
		}

		// Listen for the correct guesses to update the score
		if (_teamOneBirdButtons.Contains(note) && CurrentTeam == "A")
		{
			CheckAnswer(_teamOneBirdButtons.IndexOf(note), 1);
		}
		else if (_teamTwoBirdButtons.Contains(note) && CurrentTeam == "B")
		{
			CheckAnswer(_teamTwoBirdButtons.IndexOf(note), 2);
		}
	}

	private void CheckAnswer(int guess, int team)
    {
		if (Birds == null) return;
		_guessNumber++;
		var answer = Birds[guess];

		if (GuessedBirds != null && GuessedBirds.Contains(answer)) return;

		GuessedBirds?.Add(answer);

		// Switch off the guessed bird's light from both teams
		var lightConfig = new Dictionary<byte, byte>();
		lightConfig[(byte)_teamOneBirdButtons[guess]] = 19;
		lightConfig[(byte)_teamTwoBirdButtons[guess]] = 19;
		MidiManager.TurnOffSelectedLights(lightConfig);

		Debug.WriteLine($"Answer: {answer.CommonName}");
		UpdateTeamScore(team, answer.Sightings ?? 0);
		ShowAnswer(guess);
		PlaySoundEffect("correct");

		if (_guessNumber == 5)
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

		if (team == "A" && _teamAWrong == 3){
			GameOver("wrong");
		} else if (team == "B" && _teamBWrong == 3){
			GameOver("wrong");
		} else {
			SetCurrentTeam(CurrentTeam);
		}
	}


	private void SetupLights(Dictionary<byte, byte>? lightConfig = null)
	{
		// First turn off all lights
		MidiManager.SetupLights();

		if (lightConfig == null)
		{
			// Create light configuration for the game
			lightConfig = new Dictionary<byte, byte>();

			// Turn on team A and B bird buttons and set to green (19)
			foreach (var button in _teamOneBirdButtons)
			{
				lightConfig[(byte)button] = 19;
			}
			foreach (var button in _teamTwoBirdButtons)
			{
				lightConfig[(byte)button] = 19;
			}

			// Turn on wrong guess lights and set to red (5)
			lightConfig[34] = 5;
			lightConfig[35] = 5;
			lightConfig[36] = 5;
			lightConfig[37] = 5;
		}

		MidiManager.SetupGameLights(lightConfig);
	}

	private void GameOver(string result)
    {
        // Stub: Implement logic to handle game over
		Debug.WriteLine("Game over: " + result);

		if (result == "complete"){
			if (_teamAScore > _teamBScore){
				MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team A wins with {_teamAScore} points!")));
			} else {
				MainThread.BeginInvokeOnMainThread(() => this.ShowPopup(new PopupPage($"Team B wins with {_teamBScore} points!")));
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

		var activeTeamLabel = this.FindByName<Label>($"Team{CurrentTeam}Label");
		var inactiveTeamLabel = this.FindByName<Label>($"Team{currentTeam}Label");

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
			_teamAScore += sightings;		
		} else {
			_teamBScore += sightings;
		}
		MainThread.BeginInvokeOnMainThread(() =>
		{
			TeamAScore.Text = _teamAScore.ToString();
			TeamBScore.Text = _teamBScore.ToString();
		});
    }

    private void ShowAnswer(int index)
    {
		Debug.WriteLine("Show answer: " + index);
		var answerLabel = this.FindByName<Label>($"Answer{index.ToString()}");
		var answerSightings = this.FindByName<Label>($"Answer{index.ToString()}Sightings");
		
		if (Birds == null) return;
		MainThread.BeginInvokeOnMainThread(() =>
		{
			answerLabel.Text = Birds[index].CommonName;
			answerSightings.Text = Birds[index].Sightings.ToString();
		});
    }
    
	protected override void OnDisappearing()
	{
		MidiManager.Cleanup();
		base.OnDisappearing();
	}
}
