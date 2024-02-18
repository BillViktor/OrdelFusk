using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace OrdelFusk.Pages
{
    public partial class Index
    {
        [Inject] IToastService ToastService { get; set; }
        [Inject] HttpClient HttpClient { get; set; }

        //An array of chars for the letters and an array of string for the background color of the letters
        private char[] aLettersGuessed = new char[25];
        private string[] aColorForLetter = new string[25];

        //The current guess
        private int aCurrentGuess = 0;

        //Paths to the word lists
        private string aSwedishWordListPath = "src/swedish_five_letter_words.txt";
        private string aEnglishWordListPath = "src/english_five_letter_words.txt";

        //List of all the words
        List<string> aWordList = new List<string>();

        //Keeps track of the chosen index when choosing color
        private int aChosenIndex = 0;

        //Dictionary for letter occurences
        private Dictionary<char, int> aLetterOccurences = new Dictionary<char, int>();

        //Dictionary for the word list and their corresponding score
        private Dictionary<string, double> aWordListWithScore = new Dictionary<string, double>();

        //Flag if the website is calculating
        private bool aIsBusy = false;

        private List<char> aLettersToNotRemove = new List<char>();

        /// <summary>
        /// Sets the background color of all chars to darkgray.
        /// Reads in the words list
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            aIsBusy = true;

            //Fill the array with darkgray
            Array.Fill(aColorForLetter, "darkgray");

            //Read in the word list
            await ReadWordList();

            //Add all letters to dictionary
            InitializeDictionary();

            //Calculate probability
            CalculateProbability();

            //Count the score
            CountScoreForWord();

            aIsBusy = false;
        }

        /// <summary>
        /// Initializes the letter occurence dictionary, add all letters and set count to 0
        /// </summary>
        private void InitializeDictionary()
        {
            for (char c = 'A'; c <= 'Z'; c++)
            {
                aLetterOccurences.Add(c, 0);
            }

            //If Swedish, add Å, Ä & Ö manually
            aLetterOccurences.Add('Å', 0);
            aLetterOccurences.Add('Ä', 0);
            aLetterOccurences.Add('Ö', 0);
        }

        /// <summary>
        /// Reads the words list
        /// </summary>
        private async Task ReadWordList()
        {
            try
            {
                //Start by filling the wordslist with all words
                var fileContent = await HttpClient.GetStringAsync(aSwedishWordListPath);
                aWordList = fileContent.Split('\n').ToList();
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
            aChosenIndex = index;
        }

        /// <summary>
        /// Changes the color in the colors array
        /// </summary>
        /// <param name="index">The index in the colorforletter array to set the color to</param>
        /// <param name="color">The color to set (darkgray, purple or green)</param>
        private void ChangeColor(int index, string color)
        {
            aColorForLetter[index] = color;
        }

        /// <summary>
        /// Sets all values in the aLetterOccurences dictionary to 0 
        /// Then count the letter occurences of each word and increases the score in the aLetterOccurences
        /// </summary>
        private void CalculateProbability()
        {
            //Set the values in the dic to 0
            foreach(var key in aLetterOccurences.Keys)
            {
                aLetterOccurences[key] = 0;
            }

            //iterate all words
            foreach (var word in aWordList)
            {
                //iterate all characters in the word
                foreach (var c in word)
                {
                    if(aLetterOccurences.ContainsKey(c))
                    {
                        aLetterOccurences[c] += 1;
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
            aIsBusy = true;

            //Get the letters and the colors for the current guess
            char[] aCurrentGuessArray = aLettersGuessed.Skip(aCurrentGuess * 5).Take(5).Select(char.ToUpper).ToArray();
            string[] aCurrentColorArray = aColorForLetter.Skip(aCurrentGuess * 5).Take(5).ToArray();

            //If any input is missing, cancel method and show error
            if (aCurrentGuessArray.Any(x => x == '\0'))
            {
                ToastService.ShowError("You must fill all boxes with a character!");
                aIsBusy = false;
                return;
            }

            //Loop through all the letters
            for(int i = 0; i<aCurrentGuessArray.Length; i++)
            {
                if (aCurrentColorArray[i] == "darkgreen")
                {
                    //If letter is green, remove all letters not containing that green letter on that position
                    aWordList.RemoveAll(x => x[i] != aCurrentGuessArray[i]);

                    //Add the letter to a list to not remove it if it occurs again on gray
                    aLettersToNotRemove.Add(aCurrentGuessArray[i]);
                }
                else if (aCurrentColorArray[i] == "purple")
                {
                    //Remove all words NOT containing that letter. And also all words containing that letter in that position
                    aWordList.RemoveAll(x => !x.Contains(aCurrentGuessArray[i]) || x[i] == aCurrentGuessArray[i]);
                }
                else if (aCurrentColorArray[i] == "darkgray")
                {
                    if(aLettersToNotRemove.Contains(aCurrentGuessArray[i])) 
                    {
                        //If that letter is in the word. But not again, we remove all occurences where that letter is on the same place
                        aWordList.RemoveAll(x => x[i] == aCurrentGuessArray[i]);
                    }
                    else
                    {
                        //If the letter is truly not in the word. Remove all words with that letter
                        aWordList.RemoveAll(x => x.Contains(aCurrentGuessArray[i]));
                    }
                }
            }

            CalculateProbability();

            CountScoreForWord();

            aCurrentGuess++;
            aIsBusy = false;
        }

        /// <summary>
        /// Clears the dictionary with words and scores
        /// Then loops through the letters in the word, and gives them a score depending on the char count in the letteroccurences dic. Also only count unique letters. As it is better to guess on a word with different letters rather than a word with the same letter multiple times (for example "SATSA" is not a good word sinces there are two S and two A.
        /// </summary>
        private void CountScoreForWord()
        {
            aWordListWithScore.Clear();

            foreach (var word in aWordList)
            {
                int score = 0;
                List<char> aCheckedLetters = new List<char>();
                foreach(var c in word)
                {
                    if(aLetterOccurences.ContainsKey(c) && !aCheckedLetters.Contains(c))
                    {
                        score += aLetterOccurences[c];
                        aCheckedLetters.Add(c);
                    }
                }
                aWordListWithScore.TryAdd(word, (double)score * 100 / aLetterOccurences.Sum(x => x.Value));
            }
        }
    }
}
