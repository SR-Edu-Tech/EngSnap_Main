using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class VowelTeamsController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit5";
        [SerializeField] private string topicName = "VowelTeams";

        [Header("Data Asset")]
        [SerializeField] private VowelTeamsData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("Phase 1: Hand-in-Hand Vowel Walk UI")]
        [SerializeField] private GameObject walkingPanel;
        [SerializeField] private Button[] teamButtons = new Button[4]; // ee, ea, oa, ai
        [SerializeField] private TMP_Text[] teamTexts = new TMP_Text[4];
        [SerializeField] private Image[] pictureWordImages = new Image[4];
        [SerializeField] private TMP_Text[] pictureWordTexts = new TMP_Text[4];
        [SerializeField] private Button startSpottingPhaseButton;

        [Header("Phase 2: Team Spotting UI")]
        [SerializeField] private GameObject spottingPanel;
        [SerializeField] private VowelTeamPair spottingWordUI;
        [SerializeField] private Image spottingWordImage;

        [Header("Phase 3: Word Wall UI")]
        [SerializeField] private GameObject wordWallPanel;
        [SerializeField] private Button[] wordWallButtons;
        [SerializeField] private TMP_Text[] wordWallTexts;
        [SerializeField] private Button startWordWallButton;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;

        [Header("Rewards & Progression")]
        [Tooltip("Confetti particle system to play on completion.")]
        [SerializeField] private GameObject confettiParticles;

        [Tooltip("The sticker reward popup screen.")]
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;

        [Tooltip("The button to continue to the next activity.")]
        [SerializeField] private GameObject continueButton;

        [Tooltip("The next panel or activity to show when Continue is clicked.")]
        [SerializeField] private GameObject nextPanel;

        [Tooltip("The current panel to hide when Continue is clicked. (Assign this GameObject or its parent panel)")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentSpottingIndex = 0;
        private bool isTransitioning = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            SetupButtonListeners();
            StartActivity();
        }

        private void OnEnable()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            StartActivity();
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            for (int i = 0; i < teamButtons.Length; i++)
            {
                int index = i;
                if (teamButtons[i] != null)
                {
                    teamButtons[i].onClick.AddListener(() => OnTeamButtonClicked(index));
                }
            }

            if (startSpottingPhaseButton != null) startSpottingPhaseButton.onClick.AddListener(StartSpottingPhase);
            if (startWordWallButton != null) startWordWallButton.onClick.AddListener(StartWordWallPhase);
            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            if (wordWallButtons != null)
            {
                for (int i = 0; i < wordWallButtons.Length; i++)
                {
                    int index = i;
                    if (wordWallButtons[i] != null)
                    {
                        wordWallButtons[i].onClick.AddListener(() => OnWordWallCardClicked(index));
                    }
                }
            }
        }

        public void StartActivity()
        {
            currentSpottingIndex = 0;
            isTransitioning = false;

            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            ShowWalkingPhase();
        }

        private void ShowWalkingPhase()
        {
            if (walkingPanel != null) walkingPanel.SetActive(true);
            if (spottingPanel != null) spottingPanel.SetActive(false);
            if (wordWallPanel != null) wordWallPanel.SetActive(false);

            SetDialogue("Sometimes two vowels walk together — and only the first one talks!");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoIntroClip);
            }

            string[] teams = new string[] { "ee", "ea", "oa", "ai" };
            for (int i = 0; i < teamButtons.Length; i++)
            {
                if (teamButtons[i] != null)
                {
                    teamButtons[i].gameObject.SetActive(true);
                    if (teamTexts != null && i < teamTexts.Length && teamTexts[i] != null) teamTexts[i].text = teams[i];
                }
            }

            UpdateProgressUI(0.1f);
        }

        private void OnTeamButtonClicked(int index)
        {
            if (isTransitioning || activityData == null || activityData.vowelTeams == null) return;
            if (index < 0 || index >= activityData.vowelTeams.Length) return;

            VowelTeamItem team = activityData.vowelTeams[index];
            if (team == null) return;

            PlaySFX(activityData.starPopSfx);
            if (teamButtons[index] != null)
            {
                TriggerWiggle(teamButtons[index].GetComponent<RectTransform>());
            }

            SetDialogue($"Team '{team.teamName}' says '{team.teamSound}'! {string.Join(", ", team.pictureWordNames)}!");
            if (team.teamVoiceClip != null)
            {
                PlayVoiceClipNonBlocking(team.teamVoiceClip);
            }

            for (int p = 0; p < 4; p++)
            {
                if (p < team.pictureWordNames.Length)
                {
                    if (pictureWordTexts != null && p < pictureWordTexts.Length && pictureWordTexts[p] != null)
                    {
                        pictureWordTexts[p].text = team.pictureWordNames[p];
                    }
                    if (pictureWordImages != null && p < pictureWordImages.Length && pictureWordImages[p] != null)
                    {
                        if (team.pictureWordSprites != null && p < team.pictureWordSprites.Length && team.pictureWordSprites[p] != null)
                        {
                            pictureWordImages[p].sprite = team.pictureWordSprites[p];
                            pictureWordImages[p].gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        private void StartSpottingPhase()
        {
            if (walkingPanel != null) walkingPanel.SetActive(false);
            if (spottingPanel != null) spottingPanel.SetActive(true);
            if (wordWallPanel != null) wordWallPanel.SetActive(false);

            LoadSpottingRound(0);
        }

        private void LoadSpottingRound(int index)
        {
            if (activityData == null || activityData.spottingWords == null || index >= activityData.spottingWords.Length)
            {
                StartWordWallPhase();
                return;
            }

            currentSpottingIndex = index;
            VowelTeamSpottingWord spottingWord = activityData.spottingWords[index];

            if (spottingWordUI != null)
            {
                spottingWordUI.SetupWord(spottingWord, this);
            }

            if (spottingWordImage != null && spottingWord.wordSprite != null)
            {
                spottingWordImage.sprite = spottingWord.wordSprite;
                spottingWordImage.gameObject.SetActive(true);
            }

            SetDialogue($"Which two letters are holding hands in '{spottingWord.wordText.ToUpper()}'? Tap them!");
            if (spottingWord.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(spottingWord.wordAudioClip);
            }

            UpdateProgressUI(0.3f + (index / 6f) * 0.45f);
        }

        public void EvaluateTeamSpottingTap(VowelTeamPair tilePair, VowelTeamSpottingWord word)
        {
            if (isTransitioning || word == null) return;
            StartCoroutine(HandleTeamSpottingCorrect(tilePair, word));
        }

        private IEnumerator HandleTeamSpottingCorrect(VowelTeamPair tilePair, VowelTeamSpottingWord word)
        {
            isTransitioning = true;
            PlaySFX(activityData.handHoldLinkSfx);

            if (tilePair != null)
            {
                tilePair.PlayHandHoldLinkAnimation();
            }

            yield return new WaitForSeconds(0.4f);

            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! The two vowels in '{word.wordText.ToUpper()}' are a team!");
            yield return new WaitForSeconds(0.8f);

            currentSpottingIndex++;
            isTransitioning = false;
            LoadSpottingRound(currentSpottingIndex);
        }

        private void StartWordWallPhase()
        {
            if (walkingPanel != null) walkingPanel.SetActive(false);
            if (spottingPanel != null) spottingPanel.SetActive(false);
            if (wordWallPanel != null) wordWallPanel.SetActive(true);

            SetDialogue("Tap any card on the Vowel Teams Word Wall to hear its team sound!");

            if (activityData != null && activityData.vowelTeamsWordWallList != null && wordWallTexts != null)
            {
                for (int i = 0; i < wordWallTexts.Length; i++)
                {
                    if (i < activityData.vowelTeamsWordWallList.Length && wordWallTexts[i] != null)
                    {
                        wordWallTexts[i].text = activityData.vowelTeamsWordWallList[i];
                        if (wordWallButtons != null && i < wordWallButtons.Length && wordWallButtons[i] != null)
                        {
                            wordWallButtons[i].gameObject.SetActive(true);
                        }
                    }
                }
            }

            UpdateProgressUI(0.9f);
        }

        private void OnWordWallCardClicked(int index)
        {
            if (activityData == null || activityData.vowelTeamsWordWallList == null) return;
            if (index < 0 || index >= activityData.vowelTeamsWordWallList.Length) return;

            PlaySFX(activityData.starPopSfx);
            if (wordWallButtons != null && index < wordWallButtons.Length && wordWallButtons[index] != null)
            {
                TriggerWiggle(wordWallButtons[index].GetComponent<RectTransform>());
            }

            string word = activityData.vowelTeamsWordWallList[index];
            SetDialogue($"Vowel team sound: '{word.ToUpper()}'!");

            if (activityData.vowelTeamsWordWallClips != null && index < activityData.vowelTeamsWordWallClips.Length && activityData.vowelTeamsWordWallClips[index] != null)
            {
                PlayVoiceClipNonBlocking(activityData.vowelTeamsWordWallClips[index]);
            }
        }

        public void CompleteStop3()
        {
            StartCoroutine(CompleteStop3Sequence());
        }

        private IEnumerator CompleteStop3Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            UpdateProgressUI(1.0f);

            SetDialogue("Magic e, or a vowel team — both make the vowel say its name!");
            if (activityData != null && activityData.leoClosingClip != null)
            {
                yield return PlayVoiceClip(activityData.leoClosingClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            isTransitioning = false;
        }

        private void SetDialogue(string message)
        {
            if (dialogueText != null) dialogueText.text = message;
        }

        private void PlaySFX(AudioClip clip)
        {
            if (sfxAudioSource != null && clip != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        private void UpdateProgressUI(float fillAmount)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillAmount;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null) TriggerWiggle(starMeterRect);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target != null) StartCoroutine(WiggleAnimation(target));
        }

        private IEnumerator WiggleAnimation(RectTransform target)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 startScale = target.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed * 25f) * 0.12f;
                target.localScale = startScale * scale;
                yield return null;
            }
            target.localScale = startScale;
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                unitContentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }
    }
}
