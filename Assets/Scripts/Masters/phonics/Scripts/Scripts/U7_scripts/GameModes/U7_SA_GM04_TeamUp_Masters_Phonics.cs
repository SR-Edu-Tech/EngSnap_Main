using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM04_TeamUp_Masters_Phonics : MonoBehaviour
    {
        [Header("Item Pool (16 Items: 4 each for ai, oa, ee, i_e)")]
        public List<TeamUpItem> teamUpItems = new List<TeamUpItem>();

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private TextMeshProUGUI gappedWordText;
        [SerializeField] private Transform tilesContainer;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TextMeshProUGUI[] choiceButtonTexts;
        [SerializeField] private TextMeshProUGUI feedbackMascotText;

        [Header("Audio SFX Clips")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        private int currentIndex = 0;
        private int totalScore = 0;
        private int currentStrikes = 0;
        private int firstAttemptSuccessCount = 0;
        private bool isProcessing = false;

        [Header("Tile & Feedback Colors")]
        [Tooltip("Color of tiles in default unselected state")]
        public Color defaultTileColor = new Color(0.95f, 0.97f, 1f, 1f);
        [Tooltip("Color of tile and feedback when correct answer is chosen")]
        public Color correctColor = new Color(0.2f, 0.82f, 0.45f, 1f);
        [Tooltip("Color of tile and feedback when wrong answer is chosen")]
        public Color wrongColor = new Color(0.92f, 0.3f, 0.3f, 1f);
        [Tooltip("Color of completed word text upon correct match")]
        public Color correctTextColor = new Color(0.12f, 0.65f, 0.32f, 1f);
        [Tooltip("Dimmed color for disabled distractor tiles on Strike 2")]
        public Color dimmedDistractorColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            if (teamUpItems == null || teamUpItems.Count == 0)
            {
                teamUpItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity1Items();
            }

            currentIndex = 0;
            currentStrikes = 0;
            firstAttemptSuccessCount = 0;
            totalScore = 0;
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

            if (replayAudioBtn == null)
            {
                Transform r = transform.Find("ReplayAudioBtn") 
                           ?? transform.Find("ReplayButton") 
                           ?? transform.Find("ReplayAudioButton")
                           ?? transform.Find("Header_Container/ReplayBtn")
                           ?? transform.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? transform.Find("Speaker_Button")
                           ?? transform.Find("SpeakerButton");
                if (r != null) replayAudioBtn = r.GetComponent<Button>();
                if (replayAudioBtn == null)
                {
                    var allBtns = GetComponentsInChildren<Button>(true);
                    foreach (var b in allBtns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("speaker") || bName.Contains("replay") || bName.Contains("audio"))
                        {
                            replayAudioBtn = b;
                            break;
                        }
                    }
                }
            }

            if (illustrationImage == null)
            {
                Transform img = transform.Find("PictureWordCard/PictureDisplay")
                             ?? transform.Find("PictureWordCard/PictureImage")
                             ?? transform.Find("CardContainer/IllustrationImage") 
                             ?? transform.Find("CardContainer/PictureImage") 
                             ?? transform.Find("IllustrationImage");
                if (img != null) illustrationImage = img.GetComponent<Image>();
            }

            if (gappedWordText == null)
            {
                Transform g = transform.Find("PictureWordCard/WordDisplayRow/GappedWordText")
                           ?? transform.Find("PictureWordCard/GappedWordText")
                           ?? transform.Find("CardContainer/GappedWordText") 
                           ?? transform.Find("CardContainer/WordRow/GappedWordText") 
                           ?? transform.Find("GappedWordText");
                if (g != null) gappedWordText = g.GetComponent<TextMeshProUGUI>();
            }

            if (tilesContainer == null)
            {
                tilesContainer = transform.Find("TileOptionsContainer")
                              ?? transform.Find("TilesContainer") 
                              ?? transform.Find("ChoiceButtonsContainer") 
                              ?? transform.Find("ButtonsContainer");
            }

            if (choiceButtons == null || choiceButtons.Length == 0 || (choiceButtons.Length > 0 && choiceButtons[0] == null))
            {
                if (tilesContainer != null)
                {
                    choiceButtons = tilesContainer.GetComponentsInChildren<Button>(true);
                }
                else
                {
                    var allBtns = GetComponentsInChildren<Button>(true);
                    var list = new List<Button>();
                    foreach (var b in allBtns)
                    {
                        if (b != replayAudioBtn && (b.name.StartsWith("Tile_") || b.name.Contains("Choice") || b.name.Contains("Option")))
                        {
                            list.Add(b);
                        }
                    }
                    if (list.Count > 0) choiceButtons = list.ToArray();
                }
            }

            if (choiceButtons != null && (choiceButtonTexts == null || choiceButtonTexts.Length != choiceButtons.Length || (choiceButtonTexts.Length > 0 && choiceButtonTexts[0] == null)))
            {
                choiceButtonTexts = new TextMeshProUGUI[choiceButtons.Length];
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    if (choiceButtons[i] != null)
                    {
                        choiceButtonTexts[i] = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                }
            }

            if (feedbackMascotText == null)
            {
                Transform fb = transform.Find("FeedbackContainer/MascotText") 
                            ?? transform.Find("MascotCommentaryText")
                            ?? transform.Find("FeedbackText");
                if (fb != null) feedbackMascotText = fb.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
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

            if (teamUpItems == null || teamUpItems.Count == 0)
            {
                teamUpItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity1Items();
            }

            foreach (var item in teamUpItems)
            {
                if (item == null) continue;
                if (item.pictureSprite == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:Sprite {item.fullWord}");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        item.pictureSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                }
                if (item.wordAudio == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:AudioClip {item.fullWord}");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        item.wordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }
            }
#endif
            if (correctSFX == null) correctSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Correct");
            if (wrongSFX == null) wrongSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Incorrect");
        }

        public void LoadItem(int index)
        {
            if (teamUpItems == null || teamUpItems.Count == 0) return;

            if (index >= teamUpItems.Count)
            {
                // Activity 1 Complete -> Advance to Activity 2
                StartCoroutine(CompleteActivityRoutine());
                return;
            }

            currentIndex = index;
            currentStrikes = 0;
            isProcessing = false;

            TeamUpItem item = teamUpItems[currentIndex];

            // Display Picture
            if (illustrationImage != null)
            {
                Sprite s = item.pictureSprite != null ? item.pictureSprite : ResolveWordSprite(item.fullWord);
                if (s != null)
                {
                    illustrationImage.gameObject.SetActive(true);
                    illustrationImage.sprite = s;
                    illustrationImage.color = Color.white;
                    illustrationImage.preserveAspect = true;
                }
                else
                {
                    // Transparent when no sprite image exists so no plain white box appears
                    illustrationImage.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            // Display Gapped Word in prominent dark text
            if (gappedWordText != null)
            {
                gappedWordText.text = $"<b>{item.gappedDisplay}</b>";
                gappedWordText.color = new Color(0.06f, 0.09f, 0.16f, 1f); // Deep solid black
                gappedWordText.fontSize = 54;
                gappedWordText.fontStyle = FontStyles.Bold;
            }

            // Populate choice buttons (4 teams: ai, oa, ee, i_e) with Pure White Card & Solid Black Text
            VowelTeamType[] defaultTeams = new VowelTeamType[] { VowelTeamType.AI, VowelTeamType.OA, VowelTeamType.EE, VowelTeamType.I_E };
            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    if (choiceButtons[i] == null) continue;
                    int tileIndex = i;
                    VowelTeamType teamChoice = (item.candidateTiles != null && i < item.candidateTiles.Length) 
                        ? item.candidateTiles[i] 
                        : ((i < defaultTeams.Length) ? defaultTeams[i] : VowelTeamType.AI);

                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].interactable = true;

                    Image btnImg = choiceButtons[i].GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.color = Color.white; // Pure white card
                        btnImg.raycastTarget = true;
                    }

                    if (choiceButtonTexts != null && i < choiceButtonTexts.Length && choiceButtonTexts[i] != null)
                    {
                        choiceButtonTexts[i].text = GetTeamDisplayString(teamChoice);
                        choiceButtonTexts[i].color = new Color(0.06f, 0.09f, 0.16f, 1f); // Deep solid black text
                        choiceButtonTexts[i].fontSize = 42;
                        choiceButtonTexts[i].fontStyle = FontStyles.Bold;
                        choiceButtonTexts[i].raycastTarget = false;
                    }

                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnTileSelected(tileIndex, teamChoice));
                }
            }

            UpdateHUD();
            PlayWordAudio(item);
        }

        private string GetTeamDisplayString(VowelTeamType team)
        {
            switch (team)
            {
                case VowelTeamType.AI: return "<b>ai</b>";
                case VowelTeamType.OA: return "<b>oa</b>";
                case VowelTeamType.EE: return "<b>ee</b>";
                case VowelTeamType.I_E: return "<b>i_e</b>";
                default: return $"<b>{team.ToString().ToLower()}</b>";
            }
        }

        private void OnTileSelected(int buttonIndex, VowelTeamType chosenTeam)
        {
            if (isProcessing) return;
            isProcessing = true;

            TeamUpItem item = teamUpItems[currentIndex];
            bool isCorrect = (chosenTeam == item.correctTeam);

            StartCoroutine(EvaluateChoiceRoutine(buttonIndex, isCorrect, item));
        }

        private IEnumerator EvaluateChoiceRoutine(int buttonIndex, bool isCorrect, TeamUpItem item)
        {
            Button chosenBtn = (buttonIndex >= 0 && buttonIndex < choiceButtons.Length) ? choiceButtons[buttonIndex] : null;
            Image chosenImg = chosenBtn != null ? chosenBtn.GetComponent<Image>() : null;

            if (isCorrect)
            {
                if (currentStrikes == 0)
                {
                    firstAttemptSuccessCount++;
                }

                int points = (currentStrikes == 0) ? 50 : (currentStrikes == 1 ? 30 : 15);
                totalScore += points;

                if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(points);
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordLearned();
                }

                if (chosenImg != null) chosenImg.color = correctColor;
                if (choiceButtonTexts[buttonIndex] != null) choiceButtonTexts[buttonIndex].color = Color.white;

                // Snap team letters into gap: word completes
                if (gappedWordText != null)
                {
                    gappedWordText.text = $"<b>{item.fullWord}</b>";
                    gappedWordText.color = correctTextColor;
                }

                if (correctSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                // Spoken word plays on complete
                PlayWordAudio(item);

                ShowMascotFeedback($"Great job! <b>{item.fullWord}</b> is spelled with <b>{GetTeamDisplayString(item.correctTeam)}</b>!");

                // Punch animation on illustration
                if (illustrationImage != null)
                {
                    StartCoroutine(PunchScale(illustrationImage.transform, 1.15f, 0.25f));
                }

                yield return new WaitForSeconds(1.2f);
                LoadItem(currentIndex + 1);
            }
            else
            {
                currentStrikes++;
                if (chosenImg != null) chosenImg.color = wrongColor;

                if (wrongSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                // Word is spoken again on wrong attempt
                PlayWordAudio(item);

                // Scaffold: Strike 2 dims least likely options
                if (currentStrikes >= 2)
                {
                    DimDistractors(item.correctTeam);
                }

                ShowMascotFeedback($"Listen closely: which vowel team makes that sound in <b>{item.fullWord}</b>?");
                yield return new WaitForSeconds(0.8f);

                if (chosenImg != null) chosenImg.color = defaultTileColor;
                isProcessing = false;
            }

            UpdateHUD();
        }

        private void DimDistractors(VowelTeamType correct)
        {
            int dimmedCount = 0;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;
                VowelTeamType[] defaultTeams = new VowelTeamType[] { VowelTeamType.AI, VowelTeamType.OA, VowelTeamType.EE, VowelTeamType.I_E };
                VowelTeamType team = (i < defaultTeams.Length) ? defaultTeams[i] : VowelTeamType.AI;

                if (team != correct && dimmedCount < 2)
                {
                    choiceButtons[i].interactable = false;
                    var img = choiceButtons[i].GetComponent<Image>();
                    if (img != null) img.color = dimmedDistractorColor;
                    dimmedCount++;
                }
            }
        }

        private void UpdateHUD()
        {
            if (progressBar != null && teamUpItems.Count > 0)
            {
                progressBar.value = (float)(currentIndex + 1) / teamUpItems.Count;
            }

            if (progressText != null && teamUpItems.Count > 0)
            {
                progressText.text = $"<b>Item {currentIndex + 1} of {teamUpItems.Count}</b>";
                progressText.color = Color.white;
            }

            if (scoreText != null)
            {
                scoreText.text = $"<b>Score: {totalScore}</b>";
                scoreText.color = Color.white;
            }
        }

        private void PlayWordAudio(TeamUpItem item)
        {
            if (item == null) return;
            AudioClip clip = item.wordAudio != null ? item.wordAudio : U7_SA_AudioManager_Masters_Phonics.ResolveAudio(item.fullWord);
            if (clip != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
        }

        public void ReplayCurrentWordAudio()
        {
            if (currentIndex >= 0 && currentIndex < teamUpItems.Count)
            {
                PlayWordAudio(teamUpItems[currentIndex]);
            }
        }

        private void ShowMascotFeedback(string msg)
        {
            if (feedbackMascotText != null)
            {
                feedbackMascotText.text = msg;
            }
        }

        private Sprite ResolveWordSprite(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            string clean = word.ToLower().Trim();

            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s == null) continue;
                string n = s.name.ToLower();
                if (n == $"u07_img_{clean}" || n == clean || n.Contains($"_{clean}"))
                    return s;
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
            int earnedStars = 1;
            if (firstAttemptSuccessCount >= 14) earnedStars = 3;
            else if (firstAttemptSuccessCount >= 12) earnedStars = 2;

            if (feedbackMascotText != null)
            {
                feedbackMascotText.text = $"<b>Activity 1 Cleared! {earnedStars} Star{(earnedStars > 1 ? "s" : "")}!</b> ({firstAttemptSuccessCount}/16 first-try)";
            }

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(1, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(totalScore);
            }

            U7_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();
            yield return new WaitForSeconds(1.0f);

            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Activity 1 — Team Up", 
                earnedStars, 
                totalScore, 
                () => {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity2();
                }
            );
        }
    }
}
