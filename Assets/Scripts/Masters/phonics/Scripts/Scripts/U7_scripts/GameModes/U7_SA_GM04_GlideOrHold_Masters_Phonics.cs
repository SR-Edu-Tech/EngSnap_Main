using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM04_GlideOrHold_Masters_Phonics : MonoBehaviour
    {
        [Header("16 Items: 8 Glide / 8 Hold")]
        public List<GlideOrHoldItem> glideOrHoldItems = new List<GlideOrHoldItem>();

        [Header("UI Containers & HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button playNaturalAudioBtn;
        [SerializeField] private Button playStretchedAudioBtn;

        [Header("Central Prompt & Visual Styling")]
        [SerializeField] private TextMeshProUGUI wordPromptText;
        [SerializeField] private Image cardContainerImage;
        [SerializeField] private Image waveformImage;
        [SerializeField] private Sprite glideWaveSprite;
        [SerializeField] private Sprite holdWaveSprite;
        [SerializeField] private Sprite glideBinSprite;
        [SerializeField] private Sprite holdBinSprite;
        [SerializeField] public Color wordTextColor = new Color(0.06f, 0.09f, 0.16f, 1f); // #0F172A High-contrast Charcoal
        [SerializeField] public Color cardBgColor = new Color(1f, 1f, 1f, 0.96f);       // Crisp Clean White Card
        [SerializeField] public float wordFontSize = 58f;

        [Header("Glide vs Hold Choice Buttons")]
        [SerializeField] private Button glideButton;
        [SerializeField] private Button holdButton;
        [SerializeField] private Button glideBinGraphicButton;
        [SerializeField] private Button holdBinGraphicButton;
        [SerializeField] private TextMeshProUGUI glideButtonText;
        [SerializeField] private TextMeshProUGUI holdButtonText;
        [SerializeField] private TextMeshProUGUI mascotCommentaryText;

        [Header("Audio SFX Clips")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;
        public AudioClip waveGlideSFX;
        public AudioClip waveHoldSFX;

        private int currentIndex = 0;
        private int totalScore = 0;
        private bool isProcessing = false;

        [Header("Button & Feedback Colors")]
        [Tooltip("Color of choice buttons in default unselected state")]
        public Color defaultBtnColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("Color of choice button when correct answer is chosen")]
        public Color correctColor = new Color(0.13f, 0.77f, 0.37f, 1f); // #22C55E Vibrant Emerald
        [Tooltip("Color of choice button when wrong answer is chosen")]
        public Color wrongColor = new Color(0.94f, 0.27f, 0.27f, 1f);   // #EF4444 Vibrant Red

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            if (glideOrHoldItems == null || glideOrHoldItems.Count == 0)
            {
                glideOrHoldItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity4Items();
            }

            currentIndex = 0;
            isProcessing = false;
            LoadItem(currentIndex);
        }

        public void AutoBindHierarchyElements()
        {
            if (progressText == null)
            {
                Transform t = transform.Find("ProgressHUD/ProgressText") 
                           ?? transform.Find("ProgressHUD/Progress_Text") 
                           ?? transform.Find("ProgressHUD/ItemCounterText") 
                           ?? transform.Find("HUD/ProgressText")
                           ?? transform.Find("HUD/Progress_Text")
                           ?? transform.Find("ProgressText")
                           ?? transform.Find("Progress_Text")
                           ?? transform.Find("ItemCounterText");
                if (t != null) progressText = t.GetComponent<TextMeshProUGUI>();
            }

            // Ensure progress text is positioned cleanly above the progress bar to avoid overlapping
            if (progressText != null)
            {
                RectTransform prt = progressText.GetComponent<RectTransform>();
                if (prt != null)
                {
                    prt.anchoredPosition = new Vector2(0f, 26f);
                    prt.sizeDelta = new Vector2(500f, 35f);
                }
            }

            if (scoreText == null)
            {
                Transform t = transform.Find("ScoreHUD/ScoreText")
                           ?? transform.Find("ScoreHUD/Score_Text")
                           ?? transform.Find("HUD_Container/Score_Text") 
                           ?? transform.Find("HUD_Container/ScoreText")
                           ?? transform.Find("ProgressHUD/Score_Text") 
                           ?? transform.Find("Score_HUD/Score_Text")
                           ?? transform.Find("HUD/ScoreText")
                           ?? transform.Find("HUD/Score_Text")
                           ?? transform.Find("ScoreText")
                           ?? transform.Find("Score_Text");
                if (t != null) scoreText = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") 
                            ?? transform.Find("ProgressHUD/Progress_Bar")
                            ?? transform.Find("HUD/ProgressBar") 
                            ?? transform.Find("ProgressBar")
                            ?? transform.Find("Progress_Bar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
                if (progressBar == null) progressBar = GetComponentInChildren<Slider>(true);
            }

            if (playNaturalAudioBtn == null)
            {
                Transform r = transform.Find("PlayNaturalAudioBtn") 
                           ?? transform.Find("ReplayAudioBtn")
                           ?? transform.Find("ReplayAudioButton")
                           ?? transform.Find("ReplayButton") 
                           ?? transform.Find("Header_Container/ReplayBtn")
                           ?? transform.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? transform.Find("Speaker_Button")
                           ?? transform.Find("SpeakerButton");
                if (r != null) playNaturalAudioBtn = r.GetComponent<Button>();
                if (playNaturalAudioBtn == null)
                {
                    var allBtns = GetComponentsInChildren<Button>(true);
                    foreach (var b in allBtns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("speaker") || bName.Contains("natural") || (bName.Contains("audio") && !bName.Contains("stretch")))
                        {
                            playNaturalAudioBtn = b;
                            break;
                        }
                    }
                }
            }

            if (playStretchedAudioBtn == null)
            {
                Transform s = transform.Find("PlayStretchedAudioBtn") 
                           ?? transform.Find("CardContainer/StretchButton") 
                           ?? transform.Find("StretchBtn")
                           ?? transform.Find("StretchButton")
                           ?? transform.Find("WaveformVisual/StretchButton");
                if (s != null) playStretchedAudioBtn = s.GetComponent<Button>();
            }

            Transform cardContainer = transform.Find("WaveformVisual")
                                   ?? transform.Find("CardContainer")
                                   ?? transform.Find("AuditoryWordCard")
                                   ?? transform.Find("WordCard");
            if (cardContainer != null)
            {
                cardContainerImage = cardContainer.GetComponent<Image>();
                if (cardContainerImage != null)
                {
                    cardContainerImage.sprite = U7_SA_GM01_ConceptCards_Masters_Phonics.GetOrCreateRoundedSprite(128, 24);
                    cardContainerImage.type = Image.Type.Sliced;
                    cardContainerImage.color = cardBgColor;
                }

                RectTransform crt = cardContainer.GetComponent<RectTransform>();
                if (crt != null && crt.sizeDelta.x < 500f)
                {
                    crt.sizeDelta = new Vector2(560f, 190f);
                }
            }

            if (wordPromptText == null)
            {
                Transform w = transform.Find("WaveformVisual/WaveformText")
                           ?? transform.Find("WaveformVisual/WordText")
                           ?? transform.Find("CardContainer/WordPromptText") 
                           ?? transform.Find("CardContainer/WordText") 
                           ?? transform.Find("WordPromptText");
                if (w != null) wordPromptText = w.GetComponent<TextMeshProUGUI>();
            }

            if (waveformImage == null)
            {
                Transform wf = transform.Find("WaveformVisual/WaveformImage")
                            ?? transform.Find("WaveformVisual/WaveImage")
                            ?? transform.Find("CardContainer/WaveformImage") 
                            ?? transform.Find("WaveformImage");
                if (wf != null && wf != cardContainer) waveformImage = wf.GetComponent<Image>();
            }

            Transform buttonsGroup = transform.Find("ChoiceButtons") ?? transform.Find("ChoiceButtonsContainer") ?? transform.Find("TwoButtonsGroup") ?? transform;
            if (glideButton == null) glideButton = (buttonsGroup.Find("GlideChoiceBtn") ?? buttonsGroup.Find("GlideButton") ?? buttonsGroup.Find("Button_Glide"))?.GetComponent<Button>();
            if (holdButton == null) holdButton = (buttonsGroup.Find("HoldChoiceBtn") ?? buttonsGroup.Find("HoldButton") ?? buttonsGroup.Find("Button_Hold"))?.GetComponent<Button>();

            if (glideButton != null && glideButtonText == null) glideButtonText = glideButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (holdButton != null && holdButtonText == null) holdButtonText = holdButton.GetComponentInChildren<TextMeshProUGUI>(true);

            // Find 3D Bin Graphic GameObjects and make them directly clickable
            if (glideBinGraphicButton == null)
            {
                Transform gb = transform.Find("GlideBin") ?? transform.Find("Bin_Glide") ?? transform.Find("BinGlide") ?? buttonsGroup.Find("GlideBin") ?? buttonsGroup.Find("Bin_Glide");
                if (gb == null)
                {
                    foreach (var img in GetComponentsInChildren<Image>(true))
                    {
                        if (img.sprite != null && img.sprite.name.ToLower().Contains("bin_glide"))
                        {
                            gb = img.transform;
                            break;
                        }
                    }
                }
                if (gb != null) glideBinGraphicButton = gb.GetComponent<Button>() ?? gb.gameObject.AddComponent<Button>();
            }

            if (holdBinGraphicButton == null)
            {
                Transform hb = transform.Find("HoldBin") ?? transform.Find("Bin_Hold") ?? transform.Find("BinHold") ?? buttonsGroup.Find("HoldBin") ?? buttonsGroup.Find("Bin_Hold");
                if (hb == null)
                {
                    foreach (var img in GetComponentsInChildren<Image>(true))
                    {
                        if (img.sprite != null && img.sprite.name.ToLower().Contains("bin_hold"))
                        {
                            hb = img.transform;
                            break;
                        }
                    }
                }
                if (hb != null) holdBinGraphicButton = hb.GetComponent<Button>() ?? hb.gameObject.AddComponent<Button>();
            }

            if (mascotCommentaryText == null)
            {
                Transform m = transform.Find("FeedbackContainer/MascotText") 
                           ?? transform.Find("MascotCommentaryText");
                if (m != null) mascotCommentaryText = m.GetComponent<TextMeshProUGUI>();
            }

            if (glideWaveSprite == null) glideWaveSprite = ResolveSprite("U07_UI_Wave_Glide");
            if (holdWaveSprite == null) holdWaveSprite = ResolveSprite("U07_UI_Wave_Hold");
            if (glideBinSprite == null) glideBinSprite = ResolveSprite("U07_UI_Bin_Glide");
            if (holdBinSprite == null) holdBinSprite = ResolveSprite("U07_UI_Bin_Hold");

            if (playNaturalAudioBtn != null)
            {
                playNaturalAudioBtn.onClick.RemoveAllListeners();
                playNaturalAudioBtn.onClick.AddListener(PlayCurrentNaturalAudio);
            }

            if (playStretchedAudioBtn != null)
            {
                playStretchedAudioBtn.onClick.RemoveAllListeners();
                playStretchedAudioBtn.onClick.AddListener(PlayCurrentStretchedAudio);
            }

            if (glideButton != null)
            {
                glideButton.onClick.RemoveAllListeners();
                glideButton.onClick.AddListener(() => OnChoiceSelected(SoundMovementType.Glide));
            }

            if (glideBinGraphicButton != null)
            {
                glideBinGraphicButton.onClick.RemoveAllListeners();
                glideBinGraphicButton.onClick.AddListener(() => OnChoiceSelected(SoundMovementType.Glide));
            }

            if (holdButton != null)
            {
                holdButton.onClick.RemoveAllListeners();
                holdButton.onClick.AddListener(() => OnChoiceSelected(SoundMovementType.Hold));
            }

            if (holdBinGraphicButton != null)
            {
                holdBinGraphicButton.onClick.RemoveAllListeners();
                holdBinGraphicButton.onClick.AddListener(() => OnChoiceSelected(SoundMovementType.Hold));
            }

            if (progressBar != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            EnsureSFXClips();
        }

        private void EnsureSFXClips()
        {
#if UNITY_EDITOR
            if (correctSFX == null)
                correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3");
            if (wrongSFX == null)
                wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
#endif
            if (correctSFX == null) correctSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Correct");
            if (wrongSFX == null) wrongSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Incorrect");
            if (waveGlideSFX == null) waveGlideSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_SFX_wave_glide") ?? U7_SA_AudioManager_Masters_Phonics.ResolveAudio("wave_glide");
            if (waveHoldSFX == null) waveHoldSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_SFX_wave_hold") ?? U7_SA_AudioManager_Masters_Phonics.ResolveAudio("wave_hold");
        }

        public void LoadItem(int index)
        {
            if (glideOrHoldItems == null || glideOrHoldItems.Count == 0) return;

            if (index >= glideOrHoldItems.Count)
            {
                StartCoroutine(CompleteActivityRoutine());
                return;
            }

            currentIndex = index;
            isProcessing = false;

            GlideOrHoldItem item = glideOrHoldItems[currentIndex];

            if (cardContainerImage != null)
            {
                cardContainerImage.color = cardBgColor;
                cardContainerImage.gameObject.SetActive(true);
            }

            if (wordPromptText != null)
            {
                wordPromptText.text = $"<b><size=115%>{item.word}</size></b>\n<size=55%><color=#64748B>Listen to the vowel sound</color></size>";
                wordPromptText.fontSize = wordFontSize;
                wordPromptText.color = wordTextColor;
                wordPromptText.alignment = TextAlignmentOptions.Center;
                wordPromptText.gameObject.SetActive(true);
            }

            if (waveformImage != null)
            {
                waveformImage.gameObject.SetActive(false);
            }

            ResetButtons();
            UpdateHUD();
            PlayCurrentNaturalAudio();
        }

        private void ResetButtons()
        {
            if (glideButton != null)
            {
                glideButton.interactable = true;
                var img = glideButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = U7_SA_GM01_ConceptCards_Masters_Phonics.GetOrCreateRoundedSprite(128, 16);
                    img.type = Image.Type.Sliced;
                    img.color = defaultBtnColor;
                }
                if (glideButtonText != null)
                {
                    glideButtonText.text = "<b>GLIDE</b> <size=80%>(Diphthong)</size>";
                    glideButtonText.color = new Color(0.06f, 0.09f, 0.16f, 1f); // Solid charcoal black
                }
            }

            if (holdButton != null)
            {
                holdButton.interactable = true;
                var img = holdButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = U7_SA_GM01_ConceptCards_Masters_Phonics.GetOrCreateRoundedSprite(128, 16);
                    img.type = Image.Type.Sliced;
                    img.color = defaultBtnColor;
                }
                if (holdButtonText != null)
                {
                    holdButtonText.text = "<b>HOLD</b> <size=80%>(Steady Vowel)</size>";
                    holdButtonText.color = new Color(0.06f, 0.09f, 0.16f, 1f); // Solid charcoal black
                }
            }

            if (glideBinGraphicButton != null) glideBinGraphicButton.interactable = true;
            if (holdBinGraphicButton != null) holdBinGraphicButton.interactable = true;
        }

        private void OnChoiceSelected(SoundMovementType chosen)
        {
            if (isProcessing) return;
            isProcessing = true;

            GlideOrHoldItem item = glideOrHoldItems[currentIndex];
            bool isCorrect = (chosen == item.movementType);

            StartCoroutine(EvaluateChoiceRoutine(chosen, isCorrect, item));
        }

        private IEnumerator EvaluateChoiceRoutine(SoundMovementType chosen, bool isCorrect, GlideOrHoldItem item)
        {
            Button chosenBtn = (chosen == SoundMovementType.Glide) ? glideButton : holdButton;
            Button chosenBin = (chosen == SoundMovementType.Glide) ? glideBinGraphicButton : holdBinGraphicButton;
            Image chosenImg = chosenBtn != null ? chosenBtn.GetComponent<Image>() : null;

            if (chosenBtn != null) StartCoroutine(PunchScale(chosenBtn.transform, 1.12f, 0.2f));
            if (chosenBin != null) StartCoroutine(PunchScale(chosenBin.transform, 1.15f, 0.25f));

            if (waveformImage != null)
            {
                waveformImage.gameObject.SetActive(true);
                waveformImage.sprite = (item.movementType == SoundMovementType.Glide) ? glideWaveSprite : holdWaveSprite;
                StartCoroutine(PunchScale(waveformImage.transform, 1.15f, 0.25f));
            }

            if (isCorrect)
            {
                totalScore += 50;
                if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(50);
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordGlideIdentified();
                }

                if (chosenImg != null) chosenImg.color = correctColor;
                if (chosen == SoundMovementType.Glide && glideButtonText != null) glideButtonText.color = Color.white;
                if (chosen == SoundMovementType.Hold && holdButtonText != null) holdButtonText.color = Color.white;

                if (correctSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                // Play wave effect
                if (item.movementType == SoundMovementType.Glide && waveGlideSFX != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX(waveGlideSFX);
                else if (item.movementType == SoundMovementType.Hold && waveHoldSFX != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX(waveHoldSFX);

                if (item.movementType == SoundMovementType.Glide)
                {
                    ShowMascotCommentary($"Spot on! <b>{item.word}</b> glides ({item.ipaSymbol}) — that makes it a Diphthong!");
                }
                else
                {
                    ShowMascotCommentary($"Spot on! <b>{item.word}</b> holds steady ({item.ipaSymbol}) — no mouth movement!");
                }

                yield return new WaitForSeconds(1.4f);
                LoadItem(currentIndex + 1);
            }
            else
            {
                if (chosenImg != null) chosenImg.color = wrongColor;
                if (chosen == SoundMovementType.Glide && glideButtonText != null) glideButtonText.color = Color.white;
                if (chosen == SoundMovementType.Hold && holdButtonText != null) holdButtonText.color = Color.white;

                if (wrongSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                // Play stretched audio to illustrate
                PlayCurrentStretchedAudio();

                if (item.movementType == SoundMovementType.Glide)
                {
                    ShowMascotCommentary($"Listen to the stretched audio! <b>{item.word}</b> slides from one vowel into another (Glide).");
                }
                else
                {
                    ShowMascotCommentary($"Listen closely: in <b>{item.word}</b>, the vowel stays completely flat (Hold).");
                }

                yield return new WaitForSeconds(1.6f);
                ResetButtons();
                isProcessing = false;
            }

            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (progressBar != null && glideOrHoldItems.Count > 0)
            {
                progressBar.value = (float)currentIndex / glideOrHoldItems.Count;
            }

            if (progressText != null && glideOrHoldItems.Count > 0)
            {
                progressText.text = $"<b>Item {currentIndex + 1} of {glideOrHoldItems.Count}</b>";
                progressText.color = Color.white;
            }

            if (scoreText != null)
            {
                scoreText.text = $"<b>Score: {totalScore}</b>";
                scoreText.color = Color.white;
            }
        }

        public void PlayCurrentNaturalAudio()
        {
            if (currentIndex >= 0 && currentIndex < glideOrHoldItems.Count)
            {
                GlideOrHoldItem item = glideOrHoldItems[currentIndex];
                AudioClip clip = item.naturalAudio != null ? item.naturalAudio : U7_SA_AudioManager_Masters_Phonics.ResolveAudio(item.word);
                if (clip != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                }
            }
        }

        public void PlayCurrentStretchedAudio()
        {
            if (currentIndex >= 0 && currentIndex < glideOrHoldItems.Count)
            {
                GlideOrHoldItem item = glideOrHoldItems[currentIndex];
                AudioClip clip = item.stretchedAudio != null ? item.stretchedAudio : U7_SA_AudioManager_Masters_Phonics.ResolveAudio($"u07_str_{item.word}") ?? U7_SA_AudioManager_Masters_Phonics.ResolveAudio($"str_{item.word}");
                if (clip != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                }
                else
                {
                    PlayCurrentNaturalAudio();
                }
            }
        }

        private void ShowMascotCommentary(string msg)
        {
            if (mascotCommentaryText != null)
            {
                mascotCommentaryText.text = msg;
            }
        }

        private Sprite ResolveSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s != null && s.name.ToLower() == name.ToLower()) return s;
            }
            return null;
        }

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            Vector3 peak = orig * scale;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(orig, peak, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(peak, orig, elapsed / half);
                yield return null;
            }
            target.localScale = orig;
        }

        private IEnumerator CompleteActivityRoutine()
        {
            int earnedStars = 3;
            int score = totalScore > 0 ? totalScore : 160;

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(4, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            U7_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();
            yield return new WaitForSeconds(1.0f);

            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Activity 4 — Glide or Hold?", 
                earnedStars, 
                score, 
                () => {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity5();
                }
            );
        }
    }
}
