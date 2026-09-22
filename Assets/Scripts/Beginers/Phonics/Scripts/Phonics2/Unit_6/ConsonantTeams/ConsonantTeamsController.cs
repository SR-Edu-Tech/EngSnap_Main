using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit6
{
    public class ConsonantTeamsController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit6";
        [SerializeField] private string topicName = "ConsonantTeams";

        [Header("Data Asset")]
        [SerializeField] private ConsonantTeamsData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Hand-in-Hand Team Walk UI (4 Teams)")]
        [SerializeField] private GameObject teamWalkPanel;
        [SerializeField] private Button[] teamButtons = new Button[4]; // sh, ch, th, ng
        [SerializeField] private TMP_Text[] teamTexts = new TMP_Text[4];
        [SerializeField] private Image[] hookImages = new Image[4];
        [SerializeField] private TMP_Text[] exampleWordTexts = new TMP_Text[4];
        [SerializeField] private Image[] exampleWordImages = new Image[4];
        [SerializeField] private Button startPositionSortButton;

        [Header("Phase 2: Position Sort UI (Where does it live?)")]
        [SerializeField] private GameObject positionSortPanel;
        [SerializeField] private TMP_Text currentPosWordTMP;
        [SerializeField] private Image currentPosWordImage;
        [SerializeField] private Button startsWithButton;
        [SerializeField] private Button endsWithButton;

        [Header("Phase 3: Team Hunt UI (8 Rounds)")]
        [SerializeField] private GameObject teamHuntPanel;
        [SerializeField] private ConsonantTeamPair teamHuntWordUI;
        [SerializeField] private Image teamHuntWordImage;
        [SerializeField] private Button replayHuntAudioButton;

        [Header("Phase 4: TH Buzz Check UI (thin vs this)")]
        [SerializeField] private GameObject thCheckPanel;
        [SerializeField] private Button thinButton;
        [SerializeField] private Image thinImage;
        [SerializeField] private Button thisButton;
        [SerializeField] private Image thisImage;
        [SerializeField] private GameObject throatRippleVisual;
        [SerializeField] private Button finishStop3Button;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentTeamWalkIndex = 0;
        private int currentPosSortIndex = 0;
        private int currentHuntIndex = 0;
        private bool isTransitioning = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
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
            StopAllAudio();
            StopAllCoroutines();
            DeactivateMascots();
        }

        private void EnsureAudioSources()
        {
            if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.spatialBlend = 0f;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void EnsureDataAssigned()
        {
            if (activityData == null)
            {
                activityData = Resources.Load<ConsonantTeamsData>("Phonics2/Unit6/ConsonantTeamsData_Unit6");
            }
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
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

            if (startPositionSortButton != null) startPositionSortButton.onClick.AddListener(StartPositionSortPhase);

            if (startsWithButton != null) startsWithButton.onClick.AddListener(() => OnPositionTypeSelected(TeamPositionType.StartsWith));
            if (endsWithButton != null) endsWithButton.onClick.AddListener(() => OnPositionTypeSelected(TeamPositionType.EndsWith));

            if (replayHuntAudioButton != null) replayHuntAudioButton.onClick.AddListener(ReplayHuntAudio);

            if (thinButton != null) thinButton.onClick.AddListener(() => OnThCheckSelected(0));
            if (thisButton != null) thisButton.onClick.AddListener(() => OnThCheckSelected(1));
            if (finishStop3Button != null) finishStop3Button.onClick.AddListener(CompleteStop3);

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

        public void StartActivity()
        {
            currentTeamWalkIndex = 0;
            currentPosSortIndex = 0;
            currentHuntIndex = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (throatRippleVisual != null) throatRippleVisual.SetActive(false);

            ShowTeamWalkPhase();
        }

        #region Phase 1: Team Walk (Meet 4 Teams)

        private void ShowTeamWalkPhase()
        {
            if (teamWalkPanel != null) teamWalkPanel.SetActive(true);
            if (positionSortPanel != null) positionSortPanel.SetActive(false);
            if (teamHuntPanel != null) teamHuntPanel.SetActive(false);
            if (thCheckPanel != null) thCheckPanel.SetActive(false);
            if (startPositionSortButton != null) startPositionSortButton.gameObject.SetActive(false);

            SetDialogue("Remember when two letters held hands and made one sound? These four do it too — and now they have names!");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoIntroClip);
            }

            string[] teams = new string[] { "sh", "ch", "th", "ng" };
            for (int i = 0; i < teamButtons.Length; i++)
            {
                if (teamButtons[i] != null)
                {
                    teamButtons[i].gameObject.SetActive(true);
                    if (teamTexts != null && i < teamTexts.Length && teamTexts[i] != null) teamTexts[i].text = teams[i];
                }
            }

            UpdateProgressUI(0.05f);
        }

        private void OnTeamButtonClicked(int index)
        {
            if (isTransitioning || activityData == null || activityData.teams == null) return;
            if (index < 0 || index >= activityData.teams.Length) return;

            ConsonantTeamItem team = activityData.teams[index];

            PlaySFX(activityData.starPopSfx);
            if (teamButtons[index] != null)
            {
                StartCoroutine(PopScaleRoutine(teamButtons[index].transform, 1.25f, 0.35f));
            }

            SetDialogue($"{team.teamLetters.ToUpper()}: {team.hookPhrase} Example: {team.exampleWord.ToUpper()}!");
            if (team.hookVoiceClip != null)
            {
                PlayVoiceClipNonBlocking(team.hookVoiceClip);
            }

            if (hookImages != null && index < hookImages.Length && hookImages[index] != null && team.hookSprite != null)
            {
                hookImages[index].sprite = team.hookSprite;
                hookImages[index].gameObject.SetActive(true);
            }

            if (exampleWordTexts != null && index < exampleWordTexts.Length && exampleWordTexts[index] != null)
            {
                exampleWordTexts[index].text = team.exampleWord;
            }

            if (exampleWordImages != null && index < exampleWordImages.Length && exampleWordImages[index] != null && team.exampleWordSprite != null)
            {
                exampleWordImages[index].sprite = team.exampleWordSprite;
                exampleWordImages[index].gameObject.SetActive(true);
            }

            currentTeamWalkIndex++;
            if (currentTeamWalkIndex >= 4 && startPositionSortButton != null)
            {
                startPositionSortButton.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Phase 2: Position Sort (Starts with vs Ends with)

        private void StartPositionSortPhase()
        {
            if (teamWalkPanel != null) teamWalkPanel.SetActive(false);
            if (positionSortPanel != null) positionSortPanel.SetActive(true);
            if (teamHuntPanel != null) teamHuntPanel.SetActive(false);
            if (thCheckPanel != null) thCheckPanel.SetActive(false);

            currentPosSortIndex = 0;
            LoadPositionSortRound(0);
        }

        private void LoadPositionSortRound(int index)
        {
            if (activityData == null || activityData.positionWords == null || index >= activityData.positionWords.Length)
            {
                StartTeamHuntPhase();
                return;
            }

            currentPosSortIndex = index;
            isTransitioning = false;
            PositionSortWordItem item = activityData.positionWords[index];

            if (currentPosWordTMP != null) currentPosWordTMP.text = item.wordText;
            if (currentPosWordImage != null && item.wordSprite != null)
            {
                currentPosWordImage.sprite = item.wordSprite;
                currentPosWordImage.gameObject.SetActive(true);
            }

            SetDialogue($"Where does '{item.teamLetters}' live in '{item.wordText.ToUpper()}'? Starts with, or Ends with?");
            if (item.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordAudioClip);
            }

            UpdateProgressUI(0.3f + (index / 6f) * 0.25f);
        }

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        private void OnPositionTypeSelected(TeamPositionType chosenType)
        {
            if (isTransitioning || activityData == null || currentPosSortIndex >= activityData.positionWords.Length) return;

            PositionSortWordItem item = activityData.positionWords[currentPosSortIndex];
            bool isCorrect = (chosenType == item.positionType);
            Button chosenBtn = (chosenType == TeamPositionType.StartsWith) ? startsWithButton : endsWithButton;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();
                SetDialogue($"Correct! '{item.teamLetters}' {(chosenType == TeamPositionType.StartsWith ? "starts" : "ends")} '{item.wordText.ToUpper()}'!");
                currentPosSortIndex++;
                LoadPositionSortRound(currentPosSortIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue($"Look closely at '{item.wordText.ToUpper()}'. Is '{item.teamLetters}' at the start or end?");
            }
        }

        private IEnumerator FlashButtonFeedback(Button btn, bool isCorrect, float duration = 0.5f)
        {
            if (btn == null) yield break;
            Image img = btn.GetComponent<Image>();
            Color originalColor = (img != null) ? img.color : Color.white;
            RectTransform rt = btn.GetComponent<RectTransform>();

            if (img != null)
            {
                img.color = isCorrect ? correctGreenColor : wrongRedColor;
            }

            if (rt != null)
            {
                if (isCorrect)
                {
                    StartCoroutine(WiggleAnimation(rt));
                }
                else
                {
                    StartCoroutine(ShakeAnimation(rt));
                }
            }

            yield return new WaitForSeconds(duration);

            if (img != null)
            {
                img.color = originalColor;
            }
        }

        private IEnumerator ShakeAnimation(RectTransform target)
        {
            if (target == null) yield break;
            Vector3 startPos = target.localPosition;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 40f) * 12f * (1f - elapsed / duration);
                target.localPosition = startPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            target.localPosition = startPos;
        }

        #endregion

        #region Phase 3: Team Hunt (8 Rounds)

        private void StartTeamHuntPhase()
        {
            if (positionSortPanel != null) positionSortPanel.SetActive(false);
            if (teamHuntPanel != null) teamHuntPanel.SetActive(true);
            if (thCheckPanel != null) thCheckPanel.SetActive(false);

            currentHuntIndex = 0;
            LoadTeamHuntRound(0);
        }

        private void LoadTeamHuntRound(int index)
        {
            if (activityData == null || activityData.teamHuntWords == null || index >= activityData.teamHuntWords.Length)
            {
                StartThCheckPhase();
                return;
            }

            currentHuntIndex = index;
            isTransitioning = false;
            TeamHuntWordItem item = activityData.teamHuntWords[index];

            if (teamHuntWordUI != null)
            {
                teamHuntWordUI.SetupWord(item, this);
            }

            if (teamHuntWordImage != null && item.wordSprite != null)
            {
                teamHuntWordImage.sprite = item.wordSprite;
                teamHuntWordImage.gameObject.SetActive(true);
            }

            SetDialogue($"Which two letters are holding hands in '{item.fullWord.ToUpper()}'? Tap them!");
            if (item.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordAudioClip);
            }

            UpdateProgressUI(0.55f + (index / 8f) * 0.3f);
        }

        private void ReplayHuntAudio()
        {
            if (activityData == null || currentHuntIndex >= activityData.teamHuntWords.Length) return;
            TeamHuntWordItem item = activityData.teamHuntWords[currentHuntIndex];
            if (item != null && item.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordAudioClip);
            }
        }

        public void EvaluateTeamHuntTap(ConsonantTeamPair pairUI, TeamHuntWordItem item)
        {
            if (isTransitioning || item == null) return;
            StartCoroutine(HandleTeamHuntCorrect(pairUI, item));
        }

        private IEnumerator HandleTeamHuntCorrect(ConsonantTeamPair pairUI, TeamHuntWordItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.handHoldLinkSfx : null);

            if (pairUI != null)
            {
                pairUI.PlayHandHoldLinkAnimation();
            }

            yield return new WaitForSeconds(0.4f);

            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! '{item.teamLetters.ToUpper()}' is the team in '{item.fullWord.ToUpper()}'!");
            yield return new WaitForSeconds(0.8f);

            currentHuntIndex++;
            isTransitioning = false;
            LoadTeamHuntRound(currentHuntIndex);
        }

        #endregion

        #region Phase 4: TH Buzz Check (thin vs this)

        private void StartThCheckPhase()
        {
            if (teamHuntPanel != null) teamHuntPanel.SetActive(false);
            if (thCheckPanel != null) thCheckPanel.SetActive(true);

            SetDialogue("Here is a funny one. 'Thin' — whisper. 'This' — buzz! Hand on your throat and try both.");
            if (activityData != null && activityData.thCompareIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.thCompareIntroClip);
            }

            UpdateProgressUI(0.9f);
        }

        private void OnThCheckSelected(int index)
        {
            if (isTransitioning || activityData == null) return;
            if (activityData.thCheckWords == null || index >= activityData.thCheckWords.Length) return;

            ThBuzzCheckItem item = activityData.thCheckWords[index];
            PlaySFX(activityData.starPopSfx);

            if (item.isBuzzer)
            {
                if (throatRippleVisual != null)
                {
                    throatRippleVisual.SetActive(true);
                    StartCoroutine(PopScaleRoutine(throatRippleVisual.transform, 1.35f, 0.4f));
                }
                SetDialogue("THIS: BUZZES in your throat! Feel that vibration!");
            }
            else
            {
                if (throatRippleVisual != null) throatRippleVisual.SetActive(false);
                SetDialogue("THIN: WHISPERS! No buzz — just air!");
            }

            if (finishStop3Button != null) finishStop3Button.gameObject.SetActive(true);
        }

        #endregion

        public void CompleteStop3()
        {
            StartCoroutine(CompleteStop3Sequence());
        }

        private IEnumerator CompleteStop3Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("Four teams, four sounds. You will see these everywhere now!");
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

            yield return new WaitForSeconds(1.2f);
            isTransitioning = false;
        }

        private void SetDialogue(string message)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, message, dialogueCanvasGroup);
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
            if (starMeterRect != null) StartCoroutine(WiggleAnimation(starMeterRect));
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

        private IEnumerator PopScaleRoutine(Transform target, float targetScale, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * targetScale;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                target.localScale = Vector3.Lerp(startScale, maxScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                target.localScale = Vector3.Lerp(maxScale, startScale, t);
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

            if (nextPanel != null) nextPanel.SetActive(true);
            else if (unitContentPanel != null) unitContentPanel.SetActive(true);

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
