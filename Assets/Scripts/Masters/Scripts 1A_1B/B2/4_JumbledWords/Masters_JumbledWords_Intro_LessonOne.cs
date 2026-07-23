using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Masters_JumbledWords_Intro_LessonOne : Masters_Lesson {

    private const string END_LEVEL = "EndLevel";

    [SerializeField]
    private float timeToShowNextButton;

    [Header("Jumbled Sentence Settings")]
    [SerializeField] private TextMeshProUGUI wordPrefab;
    [SerializeField] private Transform wordsContainer;
    
    [SerializeField]
    [TextArea]
    private string introSentence = "Welcome to Jumbled Words!";
    
    [SerializeField]
    [Tooltip("Time in seconds after which the sentence gently solves itself (matching narrator voice)")]
    private float timeToSolveSentence = 4.5f;

    [SerializeField] private float floatRadius = 150f;
    [SerializeField] private float floatDuration = 2f;
    [SerializeField] private float solveDuration = 1.5f;
    [SerializeField] private float wordSpacing = 15f; 
    [SerializeField] private float lineSpacing = 80f; 

    private List<TextMeshProUGUI> spawnedWords = new List<TextMeshProUGUI>();
    private List<Vector2> targetPositions = new List<Vector2>();
    private bool isSolving = false;

    protected override void Awake() {
        base.Awake();

        float autoEndLevelTime = 20f;
        Invoke(END_LEVEL, autoEndLevelTime);

        StartCoroutine(NextButtonAnimationCoroutine());
        
        InitializeSentence();
        Invoke(nameof(SolveSentence), timeToSolveSentence);
    }

    private void InitializeSentence()
    {
        ClearWords();

        if (string.IsNullOrEmpty(introSentence)) return;

        // Split by newlines (handling both \r\n and \n)
        string[] lines = introSentence.Replace("\r", "").Split('\n');
        
        // Calculate the starting Y position to center the block vertically
        float totalHeight = (lines.Length - 1) * lineSpacing;
        float startY = totalHeight / 2f;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] words = line.Split(' ');
            
            float totalWidth = 0f;
            List<float> wordWidths = new List<float>();
            List<TextMeshProUGUI> lineWords = new List<TextMeshProUGUI>();

            // Instantiate words and calculate their widths for this line
            for (int i = 0; i < words.Length; i++)
            {
                if (string.IsNullOrEmpty(words[i])) continue;

                TextMeshProUGUI wordText = Instantiate(wordPrefab, wordsContainer);
                wordText.text = words[i];
                wordText.ForceMeshUpdate(); 
                
                float width = wordText.preferredWidth;
                wordWidths.Add(width);
                
                totalWidth += width;
                if (i < words.Length - 1)
                {
                    totalWidth += wordSpacing;
                }

                spawnedWords.Add(wordText);
                lineWords.Add(wordText);
                
                // Random floating position initially
                Vector2 randomPos = Random.insideUnitCircle * floatRadius;
                wordText.rectTransform.anchoredPosition = randomPos;
                
                // Random rotation for jumbled effect
                wordText.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
                
                // Start floating animation
                FloatWord(wordText.rectTransform);
            }

            // Calculate target positions for the solved state (centered horizontally)
            float currentX = -totalWidth / 2f;
            float currentY = startY - (lineIndex * lineSpacing);

            for (int i = 0; i < lineWords.Count; i++)
            {
                float halfWidth = wordWidths[i] / 2f;
                currentX += halfWidth;
                
                targetPositions.Add(new Vector2(currentX, currentY));
                
                currentX += halfWidth + wordSpacing;
            }
        }
    }

    private void FloatWord(RectTransform wordRect)
    {
        if (isSolving || wordRect == null) return;

        Vector2 randomTarget = wordRect.anchoredPosition + Random.insideUnitCircle * (floatRadius * 0.3f);
        float duration = floatDuration * Random.Range(0.8f, 1.2f);
        
        wordRect.DOAnchorPos(randomTarget, duration).SetEase(Ease.InOutSine).OnComplete(() => {
            if (!isSolving)
            {
                FloatWord(wordRect);
            }
        });
    }

    private void SolveSentence()
    {
        isSolving = true;
        for (int i = 0; i < spawnedWords.Count; i++)
        {
            RectTransform wordRect = spawnedWords[i].rectTransform;
            wordRect.DOKill(); // Stop floating
            
            // Move to target position, scale and rotate back to normal
            wordRect.DOAnchorPos(targetPositions[i], solveDuration).SetEase(Ease.InOutQuad);
            wordRect.DORotate(Vector3.zero, solveDuration).SetEase(Ease.InOutQuad);
        }
    }

    private void ClearWords()
    {
        isSolving = false;
        foreach (var word in spawnedWords)
        {
            if (word != null)
            {
                word.rectTransform.DOKill();
                Destroy(word.gameObject);
            }
        }
        spawnedWords.Clear();
        targetPositions.Clear();
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        ClearWords();
    }

    protected override void OnNextButtonClicked() {
        EndLevel();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private void EndLevel() {
        if(topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }

    private IEnumerator NextButtonAnimationCoroutine() {
        yield return new WaitForSeconds(timeToShowNextButton);
        NextButtonAnimation();
    }

}
