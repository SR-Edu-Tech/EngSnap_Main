using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundSafariController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Keyword Picture Display Area")]
    [SerializeField] private Image keywordDisplayImage;
    [SerializeField] private TMP_Text keywordDisplayText;
    [SerializeField] private TMP_Text phonemeDisplayText;

    [Header("Progress & Pages UI")]
    [SerializeField] private Image progressRingFillImage;
    [SerializeField] private TMP_Text progressCountText;
    [SerializeField] private Button page1Button; // A - M
    [SerializeField] private Button page2Button; // N - Z
    [SerializeField] private GameObject page1GridContainer;
    [SerializeField] private GameObject page2GridContainer;

    [Header("Grid Tiles")]
    [Tooltip("Single grid container tiles array (13 slots reused for Page 1 and 2), or specify page1Tiles and page2Tiles.")]
    [SerializeField] private SoundSafariTile[] gridTiles; // Reusable 13 slots for Page 1 & Page 2
    [SerializeField] private SoundSafariTile[] page1Tiles; // Alternative 2-container setup
    [SerializeField] private SoundSafariTile[] page2Tiles; // Alternative 2-container setup

    [Header("Data Config (26 Letters A-Z)")]
    [SerializeField] private SoundSafariData[] safariDataAZ;

    [Header("Voice Script Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip completionClip;

    [Header("Rewards & Navigation")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;

    private HashSet<int> exploredIndices = new HashSet<int>();
    private int currentPage = 1;
    private bool isTransitioning = false;
    private bool isActivityCompleted = false;

    public AudioSource SfxAudioSource => sfxAudioSource;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
        sfxAudioSource.spatialBlend = 0f;
        sfxAudioSource.volume = 1f;

        if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
        else voiceAudioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        EnsureAudioSources();

        if (page1Button != null)
        {
            page1Button.onClick.RemoveAllListeners();
            page1Button.onClick.AddListener(() => SwitchPage(1));
        }

        if (page2Button != null)
        {
            page2Button.onClick.RemoveAllListeners();
            page2Button.onClick.AddListener(() => SwitchPage(2));
        }

        if (continueButton != null)
        {
            Button btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToNextPanel);
            }
        }
    }

    private void OnEnable()
    {
        ResetLevel();
        StartCoroutine(StartIntroOnNextFrame());
    }

    private IEnumerator StartIntroOnNextFrame()
    {
        yield return null;
        if (gameObject.activeInHierarchy && !isActivityCompleted)
        {
            StartCoroutine(IntroSequence());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void ResetLevel()
    {
        StopAllCoroutines();
        exploredIndices.Clear();
        currentPage = 1;
        isTransitioning = false;
        isActivityCompleted = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);

        SetupTiles();
        UpdateProgressUI();
    }

    private void SetupTiles()
    {
        if (safariDataAZ == null) return;
        SwitchPage(1);
    }

    private void SwitchPage(int pageNum)
    {
        currentPage = pageNum;
        if (page1GridContainer != null && page2GridContainer != null)
        {
            page1GridContainer.SetActive(pageNum == 1);
            page2GridContainer.SetActive(pageNum == 2);
        }
        else if (page1GridContainer != null)
        {
            page1GridContainer.SetActive(true);
        }

        SoundSafariTile[] activeTiles = (gridTiles != null && gridTiles.Length > 0) ? gridTiles : page1Tiles;

        if (activeTiles != null && activeTiles.Length > 0)
        {
            // Single grid container reuse (e.g. 13 slots re-bound for Page 1 A-M and Page 2 N-Z)
            if (page2Tiles == null || page2Tiles.Length == 0)
            {
                int startIndex = (pageNum == 1) ? 0 : 13;
                for (int i = 0; i < activeTiles.Length; i++)
                {
                    int dataIndex = startIndex + i;
                    if (dataIndex < safariDataAZ.Length && activeTiles[i] != null)
                    {
                        activeTiles[i].gameObject.SetActive(true);
                        activeTiles[i].Setup(safariDataAZ[dataIndex], OnTileTapped);
                        if (exploredIndices.Contains(dataIndex))
                        {
                            activeTiles[i].MarkExplored();
                        }
                    }
                    else if (activeTiles[i] != null)
                    {
                        activeTiles[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // 2 separate grid tile arrays
                SoundSafariTile[] targetTiles = (pageNum == 1) ? page1Tiles : page2Tiles;
                int startIndex = (pageNum == 1) ? 0 : (page1Tiles != null ? page1Tiles.Length : 13);

                if (targetTiles != null)
                {
                    for (int i = 0; i < targetTiles.Length; i++)
                    {
                        int dataIndex = startIndex + i;
                        if (dataIndex < safariDataAZ.Length && targetTiles[i] != null)
                        {
                            targetTiles[i].Setup(safariDataAZ[dataIndex], OnTileTapped);
                            if (exploredIndices.Contains(dataIndex))
                            {
                                targetTiles[i].MarkExplored();
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetSubtitle("Welcome to the Sound Safari! Tap a letter to hear its sound.");

        if (introClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = introClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(introClip.length + 0.3f);
        }

        isTransitioning = false;
    }

    private void OnTileTapped(SoundSafariTile tile)
    {
        if (tile == null || tile.CurrentData == null) return;
        PlayTileImmediate(tile);
    }

    private void PlayTileImmediate(SoundSafariTile tile)
    {
        SoundSafariData data = tile.CurrentData;

        // Display picture and text immediately
        if (keywordDisplayImage != null && data.keywordSprite != null)
        {
            keywordDisplayImage.sprite = data.keywordSprite;
            keywordDisplayImage.enabled = true;
        }
        if (keywordDisplayText != null) keywordDisplayText.text = data.keyword;
        if (phonemeDisplayText != null) phonemeDisplayText.text = $"{data.letter} – {data.phonemeText}";

        SetSubtitle($"{data.phonemeText} – {data.keyword}");

        // Trigger tile wiggle animation immediately
        tile.PlayWiggle();

        // Mark explored and update progress UI
        int dataIndex = System.Array.IndexOf(safariDataAZ, data);
        bool isNewExploration = false;
        if (dataIndex >= 0)
        {
            isNewExploration = exploredIndices.Add(dataIndex);
        }
        UpdateProgressUI();

        // Play audio instantly without blocking future taps
        if (data.soundAndWordClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = data.soundAndWordClip;
            voiceAudioSource.Play();
        }

        // Auto redirect to Page 2 (N-Z) after all 13 letters of Page 1 (A-M, indices 0..12) are explored
        if (currentPage == 1 && isNewExploration)
        {
            int page1ExploredCount = 0;
            for (int i = 0; i < 13; i++)
            {
                if (exploredIndices.Contains(i)) page1ExploredCount++;
            }

            if (page1ExploredCount >= 13)
            {
                StartCoroutine(AutoSwitchToPage2AfterAudio(data.soundAndWordClip));
            }
        }

        // Complete ONLY when all 26 letters are explored (26/26)
        int totalTarget = (safariDataAZ != null && safariDataAZ.Length > 0) ? safariDataAZ.Length : 26;
        if (exploredIndices.Count >= totalTarget && !isActivityCompleted)
        {
            StartCoroutine(CheckCompletionAfterDelay());
        }
    }

    private IEnumerator AutoSwitchToPage2AfterAudio(AudioClip clip)
    {
        float delay = clip != null ? clip.length + 0.3f : 1.2f;
        yield return new WaitForSeconds(delay);

        if (currentPage == 1 && !isActivityCompleted)
        {
            SwitchPage(2);
        }
    }

    private IEnumerator CheckCompletionAfterDelay()
    {
        float delay = (voiceAudioSource != null && voiceAudioSource.clip != null) ? voiceAudioSource.clip.length : 1.2f;
        yield return new WaitForSeconds(delay + 0.2f);

        int totalTarget = (safariDataAZ != null && safariDataAZ.Length > 0) ? safariDataAZ.Length : 26;
        if (exploredIndices.Count >= totalTarget && !isActivityCompleted)
        {
            StartCoroutine(CompleteSequence());
        }
    }

    private void UpdateProgressUI()
    {
        int total = safariDataAZ != null ? safariDataAZ.Length : 26;
        int count = exploredIndices.Count;

        if (progressRingFillImage != null) progressRingFillImage.fillAmount = (float)count / total;
        if (progressCountText != null) progressCountText.text = $"{count}/{total}";
    }

    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;
        SetSubtitle("You found so many sounds! Well done!");

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        if (confettiParticles != null) confettiParticles.SetActive(true);
        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);

        if (completionClip != null && voiceAudioSource != null && voiceAudioSource.gameObject.activeInHierarchy)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = completionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completionClip.length + 0.3f);
        }
    }

    public void GoToNextPanel()
    {
        if (isActivityCompleted)
        {
            TopicProgressUI.MarkTopicComplete(gameObject);
        }

        ResetLevel();

        if (nextPanel != null)
        {
            nextPanel.SetActive(true);
            if (unitContentPanel != null && nextPanel != unitContentPanel && !nextPanel.transform.IsChildOf(unitContentPanel.transform))
            {
                unitContentPanel.SetActive(false);
            }
        }
        else if (unitContentPanel != null)
        {
            unitContentPanel.SetActive(true);
        }

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        TopicProgressUI.RefreshAllTicks();
    }

    private void SetSubtitle(string text)
    {
        EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
    }
}
