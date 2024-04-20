using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace OrdelFusk.Pages
{
    public partial class Index
    {
        [Inject] IToastService ToastService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] HttpClient HttpClient { get; set; }

        //Path to the word list
        private const string cSwedishWordListPath = "src/swedish_five_letter_words.txt";

        //An array of chars for the letters and an array of string for the background color of the letters
        private char[] mLettersGuessed = new char[25];
        private string[] mColorForLetter = new string[25];

        //List of all the words
        List<string> mWordList = new List<string>();

        //Dictionary for letter occurences
        private Dictionary<char, int> mLetterOccurences = new Dictionary<char, int>();

        //Dictionary for the word list and their corresponding score
        private Dictionary<string, double> mWordListWithScore = new Dictionary<string, double>();

        //Keeps track of letters not to remove
        private List<char> mLettersToNotRemove = new List<char>();

        //Keeps track of the chosen index when choosing color
        private int mChosenIndex = 0;

        //The current guess
        private int mCurrentGuess = 0;

        //Flag if the website is calculating
        private bool mIsBusy = false;

        private List<char> mCurrentGuessChars = new();

        //Valid key inputs
        private char[] aValidSwedishChars =
        [
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'Å', 'Ä', 'Ö'
        ];

        /// <summary>
        /// Sets the background color of all chars to darkgray.
        /// Reads in the words list
        /// Initializes the dictionary
        /// Calcu
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            mIsBusy = true;

            //Fill the array with darkgray
            Array.Fill(mColorForLetter, "darkgray");

            //Read in the word list
            await ReadWordList();

            //Add all letters to dictionary
            InitializeDictionary();

            //Calculate probability
            CalculateProbability();

            //Count the score
            CountScoreForWord();

            mIsBusy = false;
        }

        /// <summary>
        /// Initializes the letter occurence dictionary, add all letters and set count to 0
        /// </summary>
        private void InitializeDictionary()
        {
            for (char c = 'A'; c <= 'Z'; c++)
            {
                mLetterOccurences.Add(c, 0);
            }

            //If Swedish, add Å, Ä & Ö manually
            mLetterOccurences.Add('Å', 0);
            mLetterOccurences.Add('Ä', 0);
            mLetterOccurences.Add('Ö', 0);
        }

        /// <summary>
        /// Reads the words list
        /// </summary>
        private async Task ReadWordList()
        {
            try
            {
                //Start by filling the wordslist with all words
                var fileContent = await HttpClient.GetStringAsync(cSwedishWordListPath);
                mWordList = fileContent.Split("\r\n").ToList();
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Keeps track of the choosen cell to change color of
        /// </summary>
        /// <param name="index"></param>
        private void SetIndex(int index)
        {
            mChosenIndex = index;
        }

        /// <summary>
        /// Changes the color in the colors array
        /// </summary>
        /// <param name="index">The index in the colorforletter array to set the color to</param>
        /// <param name="color">The color to set (darkgray, purple or green)</param>
        private async Task ChangeColor(int index, string color)
        {
            mColorForLetter[index] = color;
            await SetFocus();
        }

        /// <summary>
        /// Sets all values in the aLetterOccurences dictionary to 0 
        /// Then count the letter occurences of each word and increases the score in the aLetterOccurences
        /// </summary>
        private void CalculateProbability()
        {
            //Set the values in the dic to 0
            foreach(var sKey in mLetterOccurences.Keys)
            {
                mLetterOccurences[sKey] = 0;
            }

            //iterate all words
            foreach (var sWord in mWordList)
            {
                //iterate all characters in the word
                foreach (var c in sWord)
                {
                    if(mLetterOccurences.ContainsKey(c))
                    {
                        mLetterOccurences[c] += 1;
                    }
                }
            }
        }
        

        /// <summary>
        /// Takes the chars and colors for the current guess
        /// Makes sure all inputs are done
        /// Then loops through all chars and removes word depending on the color
        /// Then calls function to calculate the letter occurences in the new word list and score
        /// </summary>
        private void Guess()
        {
            mIsBusy = true;

            //Make sure the word is valid
            string word = new string(mCurrentGuessChars.ToArray());

            //Get the letters and the colors for the current guess
            char[] aCurrentGuessArray = mCurrentGuessChars.ToArray();
            string[] aCurrentColorArray = mColorForLetter.Skip(mCurrentGuess * 5).Take(5).ToArray();

            //Loop through all the letters
            for (int i = 0; i < aCurrentGuessArray.Length; i++)
            {
                if (aCurrentColorArray[i] == "darkgreen")
                {
                    //If letter is green, remove all letters not containing that green letter on that position
                    mWordList.RemoveAll(x => x[i] != aCurrentGuessArray[i]);
                    //Add the letter to a list to not remove it if it occurs again on gray
                    mLettersToNotRemove.Add(aCurrentGuessArray[i]);
                }
                else if (aCurrentColorArray[i] == "purple")
                {
                    //Remove all words NOT containing that letter. And also all words containing that letter in that position
                    mWordList.RemoveAll(x => !x.Contains(aCurrentGuessArray[i]) || x[i] == aCurrentGuessArray[i]);
                }
                else if (aCurrentColorArray[i] == "darkgray")
                {
                    if (mLettersToNotRemove.Contains(aCurrentGuessArray[i]))
                    {
                        //If that letter is in the word. But not again, we remove all occurences where that letter is on the same place
                        mWordList.RemoveAll(x => x[i] == aCurrentGuessArray[i]);
                    }
                    else
                    {
                        //If the letter is truly not in the word. Remove all words with that letter
                        mWordList.RemoveAll(x => x.Contains(aCurrentGuessArray[i]));
                    }
                }
            }

            CalculateProbability();

            CountScoreForWord();


            //Add the chars to the array
            for (int i = 0; i < 5; i++)
            {
                mLettersGuessed[i + mCurrentGuess * 5] = aCurrentGuessArray[i];
            }

            //Reset word
            mCurrentGuessChars.Clear();

            //Increase guess count
            mCurrentGuess++;

            mIsBusy = false;
        }

        /// <summary>
        /// Clears the dictionary with words and scores
        /// Then loops through the letters in the word, and gives them a score depending on the char count in the letteroccurences dic. Also only count unique letters. As it is better to guess on a word with different letters rather than a word with the same letter multiple times (for example "SATSA" is not a good word sinces there are two S and two A.
        /// </summary>
        private void CountScoreForWord()
        {
            mWordListWithScore.Clear();

            foreach (var sWord in mWordList)
            {
                int score = 0;
                List<char> aCheckedLetters = new List<char>();
                foreach(var c in sWord)
                {
                    if(mLetterOccurences.ContainsKey(c) && !aCheckedLetters.Contains(c))
                    {
                        score += mLetterOccurences[c];
                        aCheckedLetters.Add(c);
                    }
                }
                mWordListWithScore.TryAdd(sWord, (double)score * 100 / mLetterOccurences.Sum(x => x.Value));
            }
        }

        /// <summary>
        /// Calls a JavaScript function to set the focus to the div, so the KeyDown method will be called
        /// </summary>
        /// <returns></returns>
        private async Task SetFocus()
        {
            await JSRuntime.InvokeVoidAsync("setFocus");
        }

        /// <summary>
        /// Validates the key pressed.
        /// </summary>
        /// <param name="e"></param>
        private void KeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                if (mCurrentGuessChars.Count < 5)
                {
                    ToastService.ShowError("You have to write five letters!");
                }
                else
                {
                    Guess();
                }

            }
            else if (e.Key == "Backspace")
            {
                if (mCurrentGuessChars.Count > 0)
                {
                    mCurrentGuessChars.RemoveAt(mCurrentGuessChars.Count - 1);
                }
            }

            char aCharPress;
            if (!char.TryParse(e.Key.ToUpper(), out aCharPress)) return;
            if (aValidSwedishChars.Contains(aCharPress) && mCurrentGuessChars.Count < 5)
            {
                mCurrentGuessChars.Add(aCharPress);
            }
        }
    }
}
