### Description
This is a small Blazor WASM application created to cheat on Ordel (the Swedish version of Wordle). 
Upon loading the page, the user is presented with suggested words (see the bottom of the page in the images). 
The idea is that you enter the suggested word on Ordel, and then enter the word on OrdelFusk and select the color that the letter got in Ordel.
The page will then filter out all words not matching.
The website has some other nice functionalities, for example a dark mode switch, and user input is allowed anywhere on the page just by typing on the keyboard and then pressing Enter.

### How it works
The page has a .txt file of all Swedish five letter words. 
The list is scanned and all letter occurences are counted, to figure out which letters are more probable to be in the word.
A table of suggested words is presented to the user, depending on the occurence of each letter in all Swedish five letter words (words with all unique letters are considered better than words with repeating letters in the algorithm).
On input from the user, the website filters out all words not matching the colors that the user has inputted:
* Green means that the letter is in the right place
* Purple means that the letter is in the word, but at the wrong place
* Gray means the letter is not in the word

The suggested words are always sorted on the highest score, and the score is as previously stated, based on the occurence of the letters in the word compared to all Swedish five letter words.

### Images
The page upon loading.
![image](https://github.com/BillViktor/OrdelFusk/assets/126798316/b339fa40-ff63-4d64-962e-31fd1565f6f9)

The page after filling in the results of my guess
![image](https://github.com/BillViktor/OrdelFusk/assets/126798316/4260642a-6a1a-4054-a4d4-3517faae2653)
