using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Word Hunt Grid", menuName = "Phonics/Unit 3/Word Hunt Grid Data")]
public class WordHuntGridData : ScriptableObject
{
    public int rows = 6;
    public int columns = 6;
    [TextArea(3, 5)]
    public string gridLettersFlat; // e.g., "C A T X P A N Y R A T Z F A N K"
    public List<string> targetWords = new List<string>();

    public char GetLetterAt(int r, int c)
    {
        string cleaned = (gridLettersFlat ?? "").Replace(" ", "").Replace("\n", "").Replace("\r", "").ToUpper();
        int index = r * columns + c;
        if (index >= 0 && index < cleaned.Length && char.IsLetter(cleaned[index]))
        {
            return cleaned[index];
        }
        // Fallback for missing characters so tiles are NEVER blank!
        char[] fillLetters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'M', 'N', 'P', 'R', 'S', 'T' };
        return fillLetters[(r * 7 + c * 3) % fillLetters.Length];
    }
}