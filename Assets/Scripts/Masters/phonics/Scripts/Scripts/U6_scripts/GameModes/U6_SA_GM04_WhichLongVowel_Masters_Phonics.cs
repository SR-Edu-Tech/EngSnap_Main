using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_GM04_WhichLongVowel_Masters_Phonics : MonoBehaviour
    {
        [Header("Prompt & Audio Controls")]
        [SerializeField] private Button playPromptWordBtn;
        [SerializeField] private TextMeshProUGUI promptInstructionsText;
        [SerializeField] private TextMeshProUGUI wordRevealText;

        [Header("5 Long Vowel Choice Buttons")]
        [SerializeField] private Button[] vowelButtons; // 0: LongA, 1: LongE, 2: LongI, 3: LongO, 4: LongU
        [SerializeField] private TextMeshProUGUI[] vowelButtonLabels;
        [SerializeField] private TextMeshProUGUI[] vowelPhonemeLabels;

        [Header("Vowel Reference Audio Clips (Inspector Overrides)")]
        public AudioClip longAAudio;
        public AudioClip longEAudio;
        public AudioClip longIAudio;
        public AudioClip longOAudio;
        public AudioClip longUAudio;

        [Header("HUD & Progress")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI mascotCommentaryText;
        [SerializeField] private GameObject mascotBubble;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        [Header("Colors")]
        [SerializeField] private Color normalCardColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        [SerializeField] private Color selectedPreviewColor = new Color(0.98f, 0.88f, 0.35f, 1f);
        [SerializeField] private Color correctCardColor = new Color(0.18f, 0.8f, 0.44f, 1f);
        [SerializeField] private Color wrongCardColor = new Color(0.92f, 0.3f, 0.3f, 1f);

        [Header("Item Pool")]
        [SerializeField] private List<LongVowelChoiceItem> vowelItems = new List<LongVowelChoiceItem>();

        private int currentIndex = 0;
        private int totalScore = 0;
        private bool isProcessing = false;
        private int selectedVowelIndex = -1;

        public int SelectedVowelIndex => selectedVowelIndex;

        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return proceduralRoundedSprite;
        }

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
            if (vowelItems == null || vowelItems.Count == 0)
            {
                vowelItems = U6_SA_DataTypes_Masters_Phonics.GetDefaultWhichLongVowelItems();
            }

            if (playPromptWordBtn == null)
            {
                Transform btn = transform.Find("ReplayAudioButton") ?? transform.Find("PlayPromptBtn") ?? transform.Find("AudioButton");
                if (btn != null) playPromptWordBtn = btn.GetComponent<Button>();
            }

            if (wordRevealText == null)
            {
                Transform wrd = transform.Find("WordRevealContainer/RevealedWordText") 
                             ?? transform.Find("RevealedWordText") 
                             ?? transform.Find("WordRevealText");
                if (wrd != null) wordRevealText = wrd.GetComponent<TextMeshProUGUI>();
            }

            if (vowelButtons == null || vowelButtons.Length == 0 || vowelButtons[0] == null)
            {
                Transform btnCont = transform.Find("FiveButtonsContainer") ?? transform.Find("ButtonsContainer");
                if (btnCont != null)
                {
                    List<Button> bList = new List<Button>();
                    List<TextMeshProUGUI> lList = new List<TextMeshProUGUI>();
                    for (int i = 0; i < 5; i++)
                    {
                        Transform bTr = btnCont.Find($"VowelBtn_{i}") ?? (i < btnCont.childCount ? btnCont.GetChild(i) : null);
                        if (bTr != null)
                        {
                            var b = bTr.GetComponent<Button>();
                            if (b != null)
                            {
                                bList.Add(b);
                                var tmp = bTr.GetComponentInChildren<TextMeshProUGUI>();
                                if (tmp != null) lList.Add(tmp);
                            }
                        }
                    }
                    if (bList.Count > 0)
                    {
                        vowelButtons = bList.ToArray();
                        vowelButtonLabels = lList.ToArray();
                    }
                }
            }

            if (progressText == null)
            {
                Transform pTxt = transform.Find("ProgressHUD/Progress_Text") 
                              ?? transform.Find("ProgressHUD/ProgressText") 
                              ?? transform.Find("Progress_Text") 
                              ?? transform.Find("ProgressText")
                              ?? transform.Find("ItemCounterText")
                              ?? transform.Find("Counter_Text");
                if (pTxt != null) progressText = pTxt.GetComponent<TextMeshProUGUI>();

                if (progressText == null)
                {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmps)
                    {
                        string n = tmp.gameObject.name.ToLower();
                        if ((n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("item")) &&
                            !n.Contains("score") && !n.Contains("streak") && !n.Contains("title") && !n.Contains("prompt") && !n.Contains("btn") && !n.Contains("button") && !n.Contains("bubble") && !n.Contains("feedback") && !n.Contains("vowel"))
                        {
                            progressText = tmp;
                            break;
                        }
                    }
                }
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") ?? transform.Find("ProgressBar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
            }
            if (progressBar != null) U6_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);

            if (scoreText == null)
            {
                Transform sTxt = transform.Find("ProgressHUD/Score_Text") ?? transform.Find("Score_Text");
                if (sTxt != null) scoreText = sTxt.GetComponent<TextMeshProUGUI>();
            }

            if (playPromptWordBtn != null)
            {
                playPromptWordBtn.onClick.RemoveAllListeners();
                playPromptWordBtn.onClick.AddListener(ReplayPromptWordAudio);
            }

            WireVowelButtons();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentIndex = 0;
            totalScore = 0;
            isProcessing = false;
            selectedVowelIndex = -1;

            UpdateHUD();
            LoadItem(0);
        }

        private void LoadItem(int index)
        {
            if (index >= vowelItems.Count)
            {
                FinishActivity();
                return;
            }

            currentIndex = index;
            isProcessing = false;
            selectedVowelIndex = -1;

            LongVowelChoiceItem item = vowelItems[currentIndex];

            if (wordRevealText != null)
            {
                wordRevealText.text = "((( Listen to the word )))";
                wordRevealText.fontSize = 32;
                wordRevealText.fontStyle = FontStyles.Bold;
                wordRevealText.color = new Color(0.25f, 0.45f, 0.85f, 1f);
            }

            ResetButtonColors();
            UpdateHUD();

            // Auto-play the spoken word
            PlayCurrentWordAudio(item);

            ShowMascotCommentary("Listen carefully. Which long vowel sound do you hear?");
        }

        private void WireVowelButtons()
        {
            if (vowelButtons == null || vowelButtons.Length == 0) return;

            string[] vowelNames = new string[] { "Long a", "Long e", "Long i", "Long o", "Long u" };
            string[] phonemes = new string[] { "/ay/", "/ee/", "/eye/", "/oh/", "/you/" };

            for (int i = 0; i < vowelButtons.Length; i++)
            {
                if (vowelButtons[i] != null)
                {
                    int idx = i;
                    vowelButtons[i].onClick.RemoveAllListeners();
                    vowelButtons[i].onClick.AddListener(() => OnVowelButtonClicked(idx));

                    Image img = vowelButtons[i].GetComponent<Image>();
                    if (img != null && img.sprite == null)
                    {
                        img.sprite = GetOrCreateRoundedSprite();
                        img.type = Image.Type.Sliced;
                    }

                    if (vowelButtonLabels != null && idx < vowelButtonLabels.Length && vowelButtonLabels[idx] != null)
                    {
                        vowelButtonLabels[idx].text = vowelNames[idx];
                        vowelButtonLabels[idx].fontSize = 28;
                        vowelButtonLabels[idx].fontStyle = FontStyles.Bold;
                    }

                    if (vowelPhonemeLabels != null && idx < vowelPhonemeLabels.Length && vowelPhonemeLabels[idx] != null)
                    {
                        vowelPhonemeLabels[idx].text = phonemes[idx];
                        vowelPhonemeLabels[idx].fontSize = 22;
                    }
                }
            }
        }

        private void ResetButtonColors()
        {
            if (vowelButtons == null) return;
            foreach (var btn in vowelButtons)
            {
                if (btn != null)
                {
                    Image img = btn.GetComponent<Image>();
                    if (img != null) img.color = normalCardColor;

                    TextMeshProUGUI[] texts = btn.GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (var t in texts)
                    {
                        if (t != null) t.color = new Color(0.12f, 0.18f, 0.3f, 1f);
                    }
                }
            }
        }

        private void OnVowelButtonClicked(int vowelIndex)
        {
            if (isProcessing) return;
            selectedVowelIndex = vowelIndex;

            // Play the corresponding reference vowel audio so player can compare
            PlayVowelSoundAudio(vowelIndex);
            StartCoroutine(PunchScale(vowelButtons[vowelIndex].transform, 1.1f, 0.15f));

            // Evaluate choice
            EvaluateVowelChoice(vowelIndex);
        }

        private void EvaluateVowelChoice(int chosenVowelIndex)
        {
            isProcessing = true;
            LongVowelChoiceItem item = vowelItems[currentIndex];

            int correctVowelIndex = GetVowelIndexFromType(item.targetLongVowel);
            bool isCorrect = (chosenVowelIndex == correctVowelIndex);

            Image chosenImg = vowelButtons[chosenVowelIndex].GetComponent<Image>();
            Image correctImg = vowelButtons[correctVowelIndex].GetComponent<Image>();

            if (isCorrect)
            {
                totalScore += 40;
                if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U6_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(40);

                if (chosenImg != null) chosenImg.color = correctCardColor;

                if (correctSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                if (wordRevealText != null)
                {
                    wordRevealText.text = $"<b>{item.word}</b> makes the <b>{GetVowelName(item.targetLongVowel)}</b> sound!";
                    wordRevealText.color = new Color(0.12f, 0.6f, 0.3f, 1f);
                }

                ShowMascotCommentary($"Correct! <b>{item.word}</b> has the {GetVowelName(item.targetLongVowel)} sound!");
            }
            else
            {
                totalScore += 10;
                if (chosenImg != null) chosenImg.color = wrongCardColor;
                if (correctImg != null) correctImg.color = correctCardColor;

                if (wrongSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                if (wordRevealText != null)
                {
                    wordRevealText.text = $"<b>{item.word}</b> has the <b>{GetVowelName(item.targetLongVowel)}</b> sound!";
                    wordRevealText.color = new Color(0.85f, 0.25f, 0.25f, 1f);
                }

                ShowMascotCommentary($"Listen closely: <b>{item.word}</b> makes the {GetVowelName(item.targetLongVowel)} sound!");
            }

            UpdateHUD();
            StartCoroutine(AdvanceNextItemRoutine());
        }

        private IEnumerator AdvanceNextItemRoutine()
        {
            yield return new WaitForSeconds(1.8f);
            LoadItem(currentIndex + 1);
        }

        private int GetVowelIndexFromType(VowelSoundType type)
        {
            switch (type)
            {
                case VowelSoundType.LongA: return 0;
                case VowelSoundType.LongE: return 1;
                case VowelSoundType.LongI: return 2;
                case VowelSoundType.LongO: return 3;
                case VowelSoundType.LongU: return 4;
                default: return 0;
            }
        }

        private string GetVowelName(VowelSoundType type)
        {
            switch (type)
            {
                case VowelSoundType.LongA: return "Long a (/ay/)";
                case VowelSoundType.LongE: return "Long e (/ee/)";
                case VowelSoundType.LongI: return "Long i (/eye/)";
                case VowelSoundType.LongO: return "Long o (/oh/)";
                case VowelSoundType.LongU: return "Long u (/you/)";
                default: return "Long Vowel";
            }
        }

        private void PlayCurrentWordAudio(LongVowelChoiceItem item)
        {
            if (item == null) return;
            AudioClip clip = item.wordAudio != null ? item.wordAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio(item.word);
            if (clip != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.word);
        }

        public void ReplayPromptWordAudio()
        {
            if (currentIndex < vowelItems.Count)
            {
                PlayCurrentWordAudio(vowelItems[currentIndex]);
                if (playPromptWordBtn != null)
                    StartCoroutine(PunchScale(playPromptWordBtn.transform, 1.15f, 0.15f));
            }
        }

        private void PlayVowelSoundAudio(int vowelIndex)
        {
            if (U6_SA_AudioManager_Masters_Phonics.Instance == null) return;

            AudioClip clip = null;
            switch (vowelIndex)
            {
                case 0:
                    clip = longAAudio != null ? longAAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio("cake") ?? U6_SA_AudioManager_Masters_Phonics.ResolveAudio("ay");
                    break;
                case 1:
                    clip = longEAudio != null ? longEAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio("these") ?? U6_SA_AudioManager_Masters_Phonics.ResolveAudio("ee");
                    break;
                case 2:
                    clip = longIAudio != null ? longIAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio("kite") ?? U6_SA_AudioManager_Masters_Phonics.ResolveAudio("eye");
                    break;
                case 3:
                    clip = longOAudio != null ? longOAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio("home") ?? U6_SA_AudioManager_Masters_Phonics.ResolveAudio("oh");
                    break;
                case 4:
                    clip = longUAudio != null ? longUAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio("mule") ?? U6_SA_AudioManager_Masters_Phonics.ResolveAudio("you");
                    break;
            }

            if (clip != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoicePrompt($"U05_CHK_{(char)('a' + vowelIndex)}_long");
        }

        private void ShowMascotCommentary(string msg)
        {
            if (mascotCommentaryText != null)
                mascotCommentaryText.text = msg;

            if (mascotBubble != null)
            {
                mascotBubble.SetActive(true);
                StartCoroutine(PunchScale(mascotBubble.transform, 1.05f, 0.12f));
            }
        }

        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = $"<b>Item {currentIndex + 1} of {vowelItems.Count}</b>";
                progressText.color = Color.white;
            }

            if (progressBar != null && vowelItems.Count > 0)
                progressBar.value = (float)currentIndex / vowelItems.Count;

            if (scoreText != null)
                scoreText.text = $"Score: {totalScore}";
        }

        private void FinishActivity()
        {
            if (progressBar != null) progressBar.value = 1f;
            U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("activity_complete");

            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U6_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign All Items, Sprites & Audio")]
        public void EditorAutoAssignEverything()
        {
            AutoBindHierarchyElements();

            vowelItems = U6_SA_DataTypes_Masters_Phonics.GetDefaultWhichLongVowelItems();

            for (int i = 0; i < vowelItems.Count; i++)
            {
                var item = vowelItems[i];
                if (item == null) continue;
                item.wordAudio = FindAudioInEditor(item.word);
            }

            if (longAAudio == null) longAAudio = FindAudioInEditor("cake");
            if (longEAudio == null) longEAudio = FindAudioInEditor("these");
            if (longIAudio == null) longIAudio = FindAudioInEditor("kite");
            if (longOAudio == null) longOAudio = FindAudioInEditor("home");
            if (longUAudio == null) longUAudio = FindAudioInEditor("mule");

            if (correctSFX == null) correctSFX = FindAudioInEditor("correct");
            if (wrongSFX == null) wrongSFX = FindAudioInEditor("wrong");

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=#10B981><b>[Which Long Vowel] Auto-assigned {vowelItems.Count} items & Audio Clips!</b></color>");
        }

        private AudioClip FindAudioInEditor(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            string clean = word.ToLower().Trim();

            if (clean == "correct")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3");
                if (c != null) return c;
            }
            else if (clean == "wrong" || clean == "incorrect")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
                if (c != null) return c;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new string[] { "Assets/Audio/U6_audio", "Assets/Audio", "Assets/SFX", "Assets/Resources" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename == clean || filename == $"u06_wrd_{clean}" || filename == $"u06_sfx_{clean}")
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename.Contains(clean))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }
#endif

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            target.localScale = original;
        }
    }
}
