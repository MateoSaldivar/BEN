using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class StringStandardizer {
    public static string StandardizeString(string input) {
        // Define the words to remove
        string[] wordsToRemove = { "a", "an", "the", "and", "or", "but", "of", "at", "in", "on", "for", "with", "is" };

        // Remove any non-letter characters and convert to lowercase
        string cleanedInput = Regex.Replace(input, "[^a-zA-Z]+", " ");
        string lowercaseInput = cleanedInput.ToLowerInvariant();

        // Split the string into words
        string[] words = lowercaseInput.Split(' ');

        // Remove the specified words from the word array
        words = words.Where(word => !wordsToRemove.Contains(word, StringComparer.OrdinalIgnoreCase)).ToArray();

        // Concatenate the remaining words to form the standardized string
        string standardizedString = string.Concat(words);

        return standardizedString;
    }
}
