using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_GM06_TwoMeanings_Masters_Phonics : MonoBehaviour
    {
        [Header("Sentence Card Display")]
        [SerializeField] private TextMeshProUGUI sentenceDisplayText;
        [SerializeField] private TextMeshProUGUI questionPromptText;
        [SerializeField] private Button replaySentenceAudioBtn;

        [Header("Meaning Choice Cards")]
        [SerializeField] private Button meaningCardABtn;
        [SerializeField] private Button meaningCardBBtn;
        [SerializeField] private TextMeshProUGUI meaningTextA;
        [SerializeField] private TextMeshProUGUI meaningTextB;
        [SerializeField] private Image meaningIconA;
        [SerializeField] private Image meaningIconB;

        [Header("Sprite Bank")]
        public Sprite[] homonymSprites;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<U10TwoMeaningsItem> items = new List<U10TwoMeaningsItem>();
        private int currentItemIndex = 0;
        private int firstAttemptCorrectCount = 0;
        private bool isProcessing = false;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;

            if (sentenceDisplayText == null)
            {
                Transform st = FindDeepChild(root, "SentenceText") ?? FindDeepChild(root, "Sentence") ?? root.Find("SentenceCard/Text");
                if (st != null) sentenceDisplayText = st.GetComponent<TextMeshProUGUI>();
            }

            if (questionPromptText == null)
            {
                Transform qp = FindDeepChild(root, "PromptText") ?? FindDeepChild(root, "Prompt") ?? FindDeepChild(root, "QuestionPrompt");
                if (qp != null) questionPromptText = qp.GetComponent<TextMeshProUGUI>();
            }

            if (replaySentenceAudioBtn == null)
            {
                replaySentenceAudioBtn = FindButton(root, "ReplayBtn", "AudioBtn", "Speaker_Button", "Audio_Button");
            }

            // Meanings Container
            if (meaningCardABtn == null) meaningCardABtn = FindButton(root, "MeaningCard_A", "CardA", "Btn_A", "Option_1");
            if (meaningCardBBtn == null) meaningCardBBtn = FindButton(root, "MeaningCard_B", "CardB", "Btn_B", "Option_2");

            if (meaningCardABtn != null)
            {
                if (meaningTextA == null) meaningTextA = (FindDeepChild(meaningCardABtn.transform, "Text") ?? FindDeepChild(meaningCardABtn.transform, "MeaningText"))?.GetComponent<TextMeshProUGUI>() ?? meaningCardABtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (meaningIconA == null) meaningIconA = (FindDeepChild(meaningCardABtn.transform, "Icon") ?? FindDeepChild(meaningCardABtn.transform, "Image"))?.GetComponent<Image>();
            }

            if (meaningCardBBtn != null)
            {
                if (meaningTextB == null) meaningTextB = (FindDeepChild(meaningCardBBtn.transform, "Text") ?? FindDeepChild(meaningCardBBtn.transform, "MeaningText"))?.GetComponent<TextMeshProUGUI>() ?? meaningCardBBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (meaningIconB == null) meaningIconB = (FindDeepChild(meaningCardBBtn.transform, "Icon") ?? FindDeepChild(meaningCardBBtn.transform, "Image"))?.GetComponent<Image>();
            }

            U10_UI_Utils.EnsureHUD(root, ref progressText, ref scoreText);

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (replaySentenceAudioBtn != null)
            {
                replaySentenceAudioBtn.onClick.RemoveAllListeners();
                replaySentenceAudioBtn.onClick.AddListener(ReplaySentenceAudio);
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"SentenceCard/{name}") 
                           ?? root.Find($"MeaningsContainer/{name}") 
                           ?? root.Find($"Content/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>() ?? t.GetComponentInChildren<Button>(true);
                    if (b != null) return b;
                }
            }
            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public void StartActivity()
        {
            items = U10_DataBank.GetTwoMeaningsItems();
            currentItemIndex = 0;
            firstAttemptCorrectCount = 0;
            isProcessing = false;

            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a3_intro", () =>
            {
                if (this != null && gameObject.activeInHierarchy)
                {
                    LoadItem(0);
                }
            });
        }

        private void LoadItem(int index)
        {
            if (!gameObject.activeInHierarchy) return;

            if (index >= items.Count)
            {
                OnActivityComplete();
                return;
            }

            currentItemIndex = index;
            isProcessing = false;

            U10TwoMeaningsItem item = items[index];
            UpdateHUD();

            // Voice A alert for twin second meaning
            if (index % 2 == 1)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a3_pair");
            }

            if (sentenceDisplayText != null)
            {
                // Highlight the homonym word
                string formatted = item.sentenceText.Replace(item.homonymWord, $"<color=#10B981><b>{item.homonymWord}</b></color>");
                sentenceDisplayText.text = $"<b>{formatted}</b>";
            }

            if (questionPromptText != null)
            {
                questionPromptText.text = $"<b>Which \"{item.homonymWord}\" is this?</b>";
            }

            // Dynamic Spot Illustration Resolution
            Sprite spriteA = FindIllustrationSprite(item.homonymWord, 0, item.meaningA);
            Sprite spriteB = FindIllustrationSprite(item.homonymWord, 1, item.meaningB);

            // Bind Meaning A
            if (meaningCardABtn != null)
            {
                if (meaningTextA != null) meaningTextA.text = $"<b>{item.meaningA}</b>";

                if (meaningIconA != null)
                {
                    meaningIconA.gameObject.SetActive(spriteA != null);
                    if (spriteA != null)
                    {
                        meaningIconA.sprite = spriteA;
                    }
                }

                meaningCardABtn.onClick.RemoveAllListeners();
                meaningCardABtn.onClick.AddListener(() => OnMeaningSelected(0, meaningCardABtn));
            }

            // Bind Meaning B
            if (meaningCardBBtn != null)
            {
                if (meaningTextB != null) meaningTextB.text = $"<b>{item.meaningB}</b>";

                if (meaningIconB != null)
                {
                    meaningIconB.gameObject.SetActive(spriteB != null);
                    if (spriteB != null)
                    {
                        meaningIconB.sprite = spriteB;
                    }
                }

                meaningCardBBtn.onClick.RemoveAllListeners();
                meaningCardBBtn.onClick.AddListener(() => OnMeaningSelected(1, meaningCardBBtn));
            }

            // Read the sentence
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlaySentence(item.sentenceText);
        }

        private Sprite FindIllustrationSprite(string word, int meaningIndex, string meaning)
        {
            string targetKey = GetTargetSpriteName(word, meaningIndex);
            if (string.IsNullOrEmpty(targetKey)) return null;

            // 1. Search in homonymSprites array
            if (homonymSprites != null && homonymSprites.Length > 0)
            {
                // First pass: exact match (case-insensitive)
                foreach (var s in homonymSprites)
                {
                    if (s == null) continue;
                    if (s.name.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                        return s;
                }

                // Second pass: clean normalized match (ignore spaces, underscores)
                string normTarget = targetKey.ToLower().Replace(" ", "").Replace("_", "");
                foreach (var s in homonymSprites)
                {
                    if (s == null) continue;
                    string normName = s.name.ToLower().Replace(" ", "").Replace("_", "");
                    if (normName.Contains(normTarget) || normTarget.Contains(normName))
                        return s;
                }
            }

#if UNITY_EDITOR
            // 2. Editor Fallback: Load directly from unit 10 spritesheet
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit10_MP/sprites u10 mp.png");
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is Sprite sp)
                    {
                        if (sp.name.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                            return sp;
                        string normName = sp.name.ToLower().Replace(" ", "").Replace("_", "");
                        string normTarget = targetKey.ToLower().Replace(" ", "").Replace("_", "");
                        if (normName.Contains(normTarget) || normTarget.Contains(normName))
                            return sp;
                    }
                }
            }
#endif

            return null;
        }

        private string GetTargetSpriteName(string word, int meaningIndex)
        {
            string w = word != null ? word.ToLower().Trim() : "";
            switch (w)
            {
                case "fit":
                    return meaningIndex == 0 ? "u10_fit_tantrum" : "u10_fit_shoes";
                case "swing":
                    return meaningIndex == 0 ? "u10_swing_monkeys" : "u10_swing_park";
                case "bear":
                    return meaningIndex == 0 ? "u10_bear_animal" : "u10_bear_endure";
                case "bat":
                    return meaningIndex == 0 ? "u10_bat_animal" : "u10_bat_cricket";
                case "bark":
                    return meaningIndex == 0 ? "u10_bark_dog" : "u10_bark_tree";
                case "light":
                    return meaningIndex == 0 ? "u10_light_lamp" : "u10_light_box feather";
                default:
                    return null;
            }
        }

        private void OnMeaningSelected(int selectedIdx, Button btn)
        {
            if (isProcessing) return;
            isProcessing = true;

            U10TwoMeaningsItem item = items[currentItemIndex];
            bool isCorrect = (selectedIdx == item.correctMeaningIndex);

            if (isCorrect)
            {
                firstAttemptCorrectCount++;
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(150);
                UpdateHUD();
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayMeaningFlip();

                if (btn != null)
                {
                    StartCoroutine(CoPunchTransform(btn.transform));
                }

                StartCoroutine(CoAdvance(1.2f));
            }
            else
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(item.sentenceText);

                if (btn != null)
                {
                    StartCoroutine(CoPunchTransform(btn.transform));
                }

                StartCoroutine(CoAdvance(1.8f));
            }
        }

        private void ReplaySentenceAudio()
        {
            if (currentItemIndex < items.Count)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlaySentence(items[currentItemIndex].sentenceText);
                if (replaySentenceAudioBtn != null)
                {
                    StartCoroutine(CoPunchTransform(replaySentenceAudioBtn.transform));
                }
            }
        }

        private IEnumerator CoAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadItem(currentItemIndex + 1);
        }

        private void OnActivityComplete()
        {
            int stars = 1;
            if (firstAttemptCorrectCount >= 13) stars = 3;
            else if (firstAttemptCorrectCount >= 11) stars = 2;

            int points = stars * 150;
            U10_SA_UnitFlowManager_Masters_Phonics.Instance?.ShowActivityCompletionDialog(stars, points);
        }

        private void UpdateHUD()
        {
            string progressStr = $"<b>Homonym {currentItemIndex + 1} of {items.Count}</b>";
            if (progressText != null)
            {
                progressText.text = progressStr;
            }

            var allTMP = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var pt in allTMP)
            {
                if (pt != null && (pt.name.Equals("ProgressText", StringComparison.OrdinalIgnoreCase) || 
                                   pt.name.Equals("Progress_Text", StringComparison.OrdinalIgnoreCase)))
                {
                    pt.text = progressStr;
                }
            }

            if (progressBar != null)
            {
                progressBar.value = (float)(currentItemIndex + 1) / items.Count;
            }

            if (scoreText != null)
            {
                int sc = U10_SA_UnitFlowManager_Masters_Phonics.Instance != null 
                    ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore() 
                    : 0;
                scoreText.text = $"<b>Score: {sc}</b>";
            }
        }

        private IEnumerator CoPunchTransform(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            target.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.12f);
            if (target != null) target.localScale = orig;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();

            // Load only clean, dedicated Unit 10 homonym sprites from the unit 10 spritesheet
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit10_MP/sprites u10 mp.png");
            if (assets != null && assets.Length > 0)
            {
                List<Sprite> unit10Sprites = new List<Sprite>();
                foreach (var a in assets)
                {
                    if (a is Sprite sp && sp.name.StartsWith("u10_"))
                    {
                        unit10Sprites.Add(sp);
                    }
                }
                if (unit10Sprites.Count > 0)
                {
                    homonymSprites = unit10Sprites.ToArray();
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=#10B981><b>[Unit 10 Two Meanings] Auto-Assigned Hierarchy & Cards! (Loaded {homonymSprites?.Length ?? 0} clean Unit 10 sprites)</b></color>");
        }
#endif
    }
}
