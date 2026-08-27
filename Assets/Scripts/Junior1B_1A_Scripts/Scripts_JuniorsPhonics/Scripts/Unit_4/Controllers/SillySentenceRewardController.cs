using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SillySentenceRewardController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI sentenceTitleText;
    public TextMeshProUGUI sentenceContentText;
    public Image badgeImage;
    public AudioSource audioSource;
    public AudioClip celebrationChime;

    public System.Action OnRewardComplete;

    public void Setup(Unit4LevelData levelData)
    {
        // Enforce large readable typography on ALL text components inside the reward panel!
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in allTexts)
        {
            if (t != null)
            {
                t.enableAutoSizing = true;
                t.fontSizeMin = 45;
                t.fontSizeMax = 84;
                t.fontSize = 78;
                t.fontStyle = FontStyles.Bold;
            }
        }

        if (sentenceTitleText != null)
        {
            sentenceTitleText.text = levelData != null ? $"{levelData.levelTitle} Complete!" : "Level Complete!";
            sentenceTitleText.enableAutoSizing = true;
            sentenceTitleText.fontSizeMin = 50;
            sentenceTitleText.fontSizeMax = 84;
            sentenceTitleText.fontSize = 80;
            sentenceTitleText.fontStyle = FontStyles.Bold;
        }

        if (sentenceContentText != null)
        {
            string rawSentence = levelData != null ? levelData.sillySentenceText : "";
            sentenceContentText.text = HighlightFamilyWords(rawSentence, levelData);
            sentenceContentText.enableAutoSizing = true;
            sentenceContentText.fontSizeMin = 45;
            sentenceContentText.fontSizeMax = 76;
            sentenceContentText.fontSize = 72;
            sentenceContentText.fontStyle = FontStyles.Bold;
        }

        if (badgeImage != null)
        {
            if (levelData != null && levelData.vowelBadge != null)
            {
                badgeImage.gameObject.SetActive(true);
                badgeImage.sprite = levelData.vowelBadge;
            }
            else
            {
                badgeImage.gameObject.SetActive(false); // Hide blank white square!
            }
        }

        StartCoroutine(RewardRoutine(levelData));
    }

    private IEnumerator RewardRoutine(Unit4LevelData levelData)
    {
        // 0. Stop any leftover sound currently playing from Activity 3
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayCelebrationAnimation();

        if (audioSource != null && levelData != null)
        {
            // 1. Play the Silly Sentence Audio first
            if (levelData.sillySentenceAudio != null)
            {
                audioSource.clip = levelData.sillySentenceAudio;
                audioSource.Play();

                // Wait until sentence clip finishes completely
                while (audioSource.isPlaying)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(0.4f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            // 2. Play Congratulations Audio ONLY after sentence clip has finished
            AudioClip congratsClip = null;
#if UNITY_EDITOR
            string congratsPath = "Assets/Audio Clips/unit 5/dialogues/You know this family now Here's your badge.mp3";
            congratsClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(congratsPath);
#endif
            if (congratsClip != null)
            {
                audioSource.clip = congratsClip;
                audioSource.Play();

                while (audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else if (celebrationChime != null)
            {
                audioSource.PlayOneShot(celebrationChime);
            }
        }
    }

    private string HighlightFamilyWords(string originalSentence, Unit4LevelData levelData)
    {
        if (string.IsNullOrEmpty(originalSentence)) return "";

        string formattedSentence = originalSentence;
        List<string> targetWords = new List<string>();

        if (levelData != null && levelData.families != null)
        {
            foreach (var family in levelData.families)
            {
                if (family != null && family.familyWords != null)
                {
                    foreach (var wData in family.familyWords)
                    {
                        if (wData != null && !string.IsNullOrEmpty(wData.word) && !targetWords.Contains(wData.word.ToLower()))
                        {
                            targetWords.Add(wData.word.ToLower());
                        }
                    }
                }
            }
        }

        // Fallback Short A target words if families list is empty
        if (targetWords.Count == 0)
        {
            targetWords.AddRange(new string[] { "cat", "ran", "at", "man", "sat", "mat", "rat", "hat", "can", "pan", "fan", "dad", "sad", "mad", "ram", "cap", "map" });
        }

        // Sort by word length descending so longer words like "cat" get formatted before "at"
        targetWords.Sort((a, b) => b.Length.CompareTo(a.Length));

        foreach (string target in targetWords)
        {
            string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(target) + @"\b";
            formattedSentence = System.Text.RegularExpressions.Regex.Replace(
                formattedSentence,
                pattern,
                $"<color=#FF2244><b>{target}</b></color>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        return formattedSentence;
    }
}
