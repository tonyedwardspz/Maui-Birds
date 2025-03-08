using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using Maui_Birds.Helpers;
using Maui_Birds.Midi;
using Maui_Birds.Models;

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
    private List<int> teamTwoBirdButtons = new List<int> { 7, 15, 23, 31, 39 };

	private List<int> guesses = new List<int>();
    private int guessNumber = 0;
    private int lastPad;

	public int teamAScore = 0;
	public int teamBScore = 0;


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
		// sort the birds by sightings
		Birds = Birds.OrderByDescending(b => b.Sightings).ToList().Take(5).ToList();
	}

	private async Task InitializeMidiAsync()
	{
		var inputs = MidiManager.AvailableInputDevices;
		await MidiManager.EnsureInputReady("APC Key 25");

		MidiManager.ActiveInputDevices["APC Key 25"].NoteOn += HandleNoteOn;
		MidiManager.ActiveInputDevices["APC Key 25"].NoteOff += HandleNoteOff;

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
			if (note == 48){
				// Team A hit first
				Debug.WriteLine("Team A hit first");
				CurrentTeam = "A";
				hasGameStarted = true;
			} else if (note == 72){
				// Team B hit first
				Debug.WriteLine("Team B hit first");
				hasGameStarted = true;
			}
			return;
		}

		if (hasGameStarted && !teamSelected){
			if (teamOneBirdButtons.Contains(note) && CurrentTeam == "A")
			{
				int index = teamOneBirdButtons.IndexOf(note);
				CheckAnswer(index, 1);
				ShowAnswer(index);
			}
			else if (teamTwoBirdButtons.Contains(note) && CurrentTeam == "B")
			{
				int index = teamTwoBirdButtons.IndexOf(note);
				CheckAnswer(index, 2);
				ShowAnswer(index);
			}
			return;
		}

		if (hasGameStarted && teamSelected){
			
			// Game on
			var bird = Birds.FirstOrDefault(b => b.Id == note);







			MainThread.BeginInvokeOnMainThread(() =>
			{
				string filename = $"{bird.CommonName.Replace(" ", "_").ToLower()}.mp3";

				if (FileSystem.AppPackageFileExistsAsync(filename).Result)
				{
					MediaSource source = MediaSource.FromResource(filename);
					BirdSongPlayer.Source = source;
				}
				else
				{
					MediaSource source = MediaSource.FromResource("placeholder.wav");
					BirdSongPlayer.Source = source;
				}
				BirdSongPlayer.Play();
			});

		}
	}

	private void CheckAnswer(int guess, int team)
    {
        if (!guesses.Contains(guess))
        {
            // PlaySound("./audio/effects/correct.mp3");
            guessNumber++;
            var answer = Birds[guess];
            guesses.Add(guess);

            Console.WriteLine($"Answer: {answer.CommonName}");
            UpdateTeamScore(team, answer.Sightings ?? 0);
            ShowAnswer(guess);
            // RemoveLight(lastPad);

            if (guessNumber == 5)
            {
                // GameOver("complete");
				Debug.WriteLine("Game over");
				GameOver("complete");
            }
        }
    }

	private void WrongGuess(string team)
	{
		Debug.WriteLine("Wrong guess: " + team);
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
			Debug.WriteLine("Team A score updated: " + teamAScore);
		} else {
			teamBScore += sightings;
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
			BirdSongPlayer.Stop();
		});
	}
}
