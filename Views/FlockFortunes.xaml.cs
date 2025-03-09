using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Helpers;
using Maui_Birds.Midi;
using Maui_Birds.Models;
using Microsoft.VisualBasic;
using FileSystem = Microsoft.Maui.Storage.FileSystem;

namespace Maui_Birds.Views;

public partial class FlockFortunes : ContentPage
{
	private List<Bird> _birds;
	public List<Bird> Birds
	{
		get => _birds;
		set => _birds = value;
	}

	private bool hasGameStarted = false;
	private bool teamSelected = false;
	public string CurrentTeam { get; set; } = "A";

	private List<int> teamOneBirdButtons = new List<int> { 32, 24, 16, 8, 0 };
    private List<int> teamTwoBirdButtons = new List<int> { 39, 31, 23, 15, 7 };

	private List<int> guesses = new List<int>();
    private int guessNumber = 0;
    private int lastPad;

	public int teamAScore = 0;
	public int teamBScore = 0;

	public int teamAWrong = 0;
	public int teamBWrong = 0;

	public FlockFortunes()
	{
		InitializeComponent();
		BindingContext = this;

		LoadBirdsAsync();
		InitializeMidiAsync();
	}

	private async Task LoadBirdsAsync()
	{
		Birds = await BirdHelper.LoadConfig("data.json");
		Birds = Birds.OrderByDescending(b => b.Sightings).ToList().Take(5).ToList();
	}

	private async Task InitializeMidiAsync()
	{
		var inputs = MidiManager.AvailableInputDevices;
		await MidiManager.EnsureInputReady("APC Key 25");



		MidiManager.ActiveInputDevices["APC Key 25"].NoteOn += HandleNoteOn;
		// MidiManager.ActiveInputDevices["APC Key 25"].NoteOff += HandleNoteOff;

		// var outputs = MidiManager.AvailableOutputDevices;
		// Debug.WriteLine(outputs.Count);

		// await MidiManager.EnsureOutputReady("APC Key 25");
		// await MidiManager.OpenOutput("APC Key 25");
		// MidiManager.ActiveOutputDevices["APC Key 25"].Send(new byte[] { 0, 48, 127 }, 0, 3, 0);
	}

	private void HandleNoteOn(int note, int velocity)
	{
		Debug.WriteLine($"Note on from Flock game: {note}");
		
		// Listen for the starting team key 48 / 72
		if (!hasGameStarted && !teamSelected)
		{
			PlaySoundEffect("team_buzzer");
			if (note == 48){
				// Team A hit first
				Debug.WriteLine("Team A hit first");
				CurrentTeam = "A";
				hasGameStarted = true;
			} else if (note == 72){
				// Team B hit first
				Debug.WriteLine("Team B hit first");
				CurrentTeam = "B";
				hasGameStarted = true;
			}
			return;
		}

		// Listen for the first guess to decide in play team
		if (hasGameStarted && !teamSelected){
			if (teamOneBirdButtons.Contains(note) && CurrentTeam == "A")
			{
				int index = teamOneBirdButtons.IndexOf(note);
				CheckAnswer(index, 1);
				CurrentTeam = "A";
				teamSelected = true;
			}
			else if (teamTwoBirdButtons.Contains(note) && CurrentTeam == "B")
			{
				int index = teamTwoBirdButtons.IndexOf(note);
				CheckAnswer(index, 2);
				CurrentTeam = "B";
				teamSelected = true;
			}
			return;
		}

		// Listen for the wrong guesses to swap teams
		if (note == 34 || note == 35){
			teamAWrong++;
			WrongGuess("B", "TeamAWrong" + teamAWrong.ToString());
			return;
		} 
		else if (note == 36 || note == 37) {
			teamBWrong++;
			WrongGuess("A", "TeamBWrong" + teamBWrong.ToString());
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
        if (!guesses.Contains(guess))
        {
            guessNumber++;
            var answer = Birds[guess];
            guesses.Add(guess);

            Debug.WriteLine($"Answer: {answer.CommonName}");
            UpdateTeamScore(team, answer.Sightings ?? 0);
            ShowAnswer(guess);
			PlaySoundEffect("correct");
            // RemoveLight(lastPad);

            if (guessNumber == 5)
            {
                // GameOver("complete");
				Debug.WriteLine("Game over");
				GameOver("complete");
            }
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

		// swap the team using a ternary operator
		CurrentTeam = CurrentTeam == "A" ? "B" : "A";
	}

	private void GameOver(string result)
    {
        // Stub: Implement logic to handle game over
		Debug.WriteLine("Game over: " + result);
    }

    private void UpdateTeamScore(int team, int sightings)
    {
        // Stub: Implement logic to update team score
		Debug.WriteLine("Update team score: " + team + " " + sightings);
		if (team == 1){
			teamAScore += sightings;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				TeamAScore.Text = teamAScore.ToString();
			});
			
			Debug.WriteLine("Team A score updated: " + teamAScore);
		} else {
			teamBScore += sightings;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				TeamBScore.Text = teamBScore.ToString();
			});
			Debug.WriteLine("Team B score updated: " + teamBScore);
		}
    }

    private void ShowAnswer(int index)
    {
		Debug.WriteLine("Show answer: " + index);
		Label AnswerLabel = this.FindByName<Label>($"Answer{index.ToString()}");
		MainThread.BeginInvokeOnMainThread(() =>
		{
			AnswerLabel.Text = Birds[index].CommonName + " " + Birds[index].Sightings;
		});
    }

	private void HandleNoteOff(int note)
	{
		Debug.WriteLine($"Note off from flock game: {note}");
		MainThread.BeginInvokeOnMainThread(() =>
		{
			AudioPlayer.Stop();
		});
	}
}
