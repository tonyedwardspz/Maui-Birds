using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maui_Birds;
using CommunityToolkit.Maui.Views;

namespace Maui_Birds.Views;

public partial class WheelOfFeathersView : ContentPage
{
    private readonly Wheel wheel;
    private string currentPhrase;
    private List<char> guessedLetters;
    private int currentPlayer;
    private readonly string[] phrases = new[]
    {
        "A BIRD IN THE HAND",
        "THE EARLY BIRD GETS THE WORM",
        "BIRDS OF A FEATHER FLOCK TOGETHER",
        "KILL TWO BIRDS WITH ONE STONE",
        "A LITTLE BIRD TOLD ME",
        "NULL"
    };

    public WheelOfFeathersView()
    {
        InitializeComponent();
        BindingContext = this;

        wheel = new Wheel(this);
        WheelView.Drawable = wheel;

        wheel.Finished += WheelOnFinished;
    }

    private void SetFocusOnLetterEntry()
    {
        LetterEntry.Focus();
    }

    private void StartNewGame()
    {
        var random = new Random();
        currentPhrase = phrases[random.Next(phrases.Length)];
        
        guessedLetters = new List<char>();
        currentPlayer = 1;
        
        UpdateCurrentPlayerLabel();
        UpdatePhraseGrid();
        LetterEntry.Text = string.Empty;
        
        // Set focus on the letter entry
        SetFocusOnLetterEntry();
    }

    private void UpdateCurrentPlayerLabel()
    {
        CurrentPlayerLabel.Text = $"Player {currentPlayer}'s Turn";
    }

    private void UpdatePhraseGrid()
    {
        PhraseGrid.Children.Clear();
        PhraseGrid.RowDefinitions.Clear();
        PhraseGrid.ColumnDefinitions.Clear();

        // Create grid layout based on phrase
        var words = currentPhrase.Split(' ');
        int maxWordLength = words.Max(w => w.Length);
        
        // Set up grid with 12 columns
        for (int i = 0; i < 12; i++)
        {
            PhraseGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        int currentRow = 0;
        int currentColumn = 0;
        PhraseGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        foreach (var word in words)
        {
            // Check if we need to start a new row
            if (currentColumn + word.Length > 12)
            {
                currentRow++;
                currentColumn = 0;
                PhraseGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Add each letter of the word
            for (int i = 0; i < word.Length; i++)
            {
                var letter = word[i];
                var label = new Label
                {
                    Text = guessedLetters.Contains(letter) ? letter.ToString() : "_",
                    FontSize = 42,
                    WidthRequest = 40,
                    HorizontalOptions = LayoutOptions.Center
                };

                PhraseGrid.Add(label, currentColumn, currentRow);
                currentColumn++;
            }

            // Add space after word if not at the end of the row
            if (currentColumn < 12 && word != words.Last())
            {
                currentColumn++;
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

    private void OnGuessButtonClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LetterEntry.Text))
            return;

        char guessedLetter = char.ToUpper(LetterEntry.Text[0]);
        
        if (guessedLetters.Contains(guessedLetter))
        {
            DisplayAlert("Already Guessed", "This letter has already been guessed!", "OK");
            LetterEntry.Text = string.Empty;
            SetFocusOnLetterEntry();
            return;
        }

        guessedLetters.Add(guessedLetter);
        // UsedLettersLabel.Text = string.Join(" ", guessedLetters.OrderBy(c => c));
        
        // Check if the letter is in the phrase
        bool isCorrect = currentPhrase.Contains(guessedLetter);
        
        if (!isCorrect)
        {
            // Move to next player
            PlaySoundEffect("letter_wrong");
            currentPlayer = currentPlayer % 3 + 1;
            UpdateCurrentPlayerLabel();
        }

        UpdatePhraseGrid();
        LetterEntry.Text = string.Empty;
        SetFocusOnLetterEntry();

        // Check if game is won
        if (IsGameWon())
        {
            PlaySoundEffect("puzzle_complete");
            DisplayAlert("Game Over", $"Player {currentPlayer} wins!", "OK");
            StartNewGame();
        } else if (isCorrect) {
            PlaySoundEffect("letter_reveal");
        }
    }

    private bool IsGameWon()
    {
        return currentPhrase.All(c => c == ' ' || guessedLetters.Contains(c));
    }

    private void OnNewGameButtonClicked(object sender, EventArgs e)
    {
        PlaySoundEffect("puzzle_reveal");
        StartNewGame();
        
        this.wheel.UpdateNames();
        this.Invalidate();
    }

    private void OnLetterEntryCompleted(object sender, EventArgs e)
    {
        OnGuessButtonClicked(sender, e);
    }

    private async void WheelOnFinished(string obj)
    {
        await DisplayAlert("And the winner is", obj, "OK");
    }

    private void Wheel_OnStartInteraction(object? sender, TouchEventArgs e)
    {
        Debug.WriteLine("Spinning wheel. . . ");
        wheel.Spin();
    }

    internal void Invalidate()
    {
        this.WheelView.Invalidate();
    }

    private void NamesEditor_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        this.wheel.UpdateNames();
        this.Invalidate();
    }
}