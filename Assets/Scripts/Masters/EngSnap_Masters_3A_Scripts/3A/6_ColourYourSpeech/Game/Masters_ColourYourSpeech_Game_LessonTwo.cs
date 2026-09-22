using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller for Unit 6: Colour Your Speech - Game Lesson Two (G02: Idiom Memory Match).
/// Reuses the proven Masters_BoostSomeoneUp_Game_LessonTwo memory-game architecture.
/// </summary>
public class Masters_ColourYourSpeech_Game_LessonTwo : Masters_BoostSomeoneUp_Game_LessonTwo {

    [Header("Unit 6 Voice & Audio")]
    [Tooltip("Aria's intro voiceover for G02: 'Match each idiom to what it really means!'")]
    [SerializeField] private AudioClip voAria;

    [Tooltip("Optional custom card flip SFX (falls back to Masters_SFX.Pop).")]
    [SerializeField] private AudioClip sfxFlip;

    private Coroutine matchCheckCoroutine;

    protected override void Start() {
        base.Start();

        PlayIntroAudio();
    }

    private void PlayIntroAudio() {
        if (voAria != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(voAria);
        } else if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    protected override void StartGame() {
        if (matchCheckCoroutine != null) {
            StopCoroutine(matchCheckCoroutine);
            matchCheckCoroutine = null;
        }

        base.StartGame();
        UpdateUI();
    }

    protected new void UpdateUI() {
        if (scoreTMP != null) {
            int total = matchPairs != null ? matchPairs.Count : 6;
            scoreTMP.text = $"{matchedPairsCount}/{total}";
        }
    }

    public override void TileSelected(Masters_GenericMemoryTile selectedTile) {
        if (!CanSelectTile() || selectedTile == null) return;

        // Play card flip SFX
        if (sfxFlip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxFlip);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        if (firstSelectedTile == null) {
            firstSelectedTile = selectedTile;
        } else if (secondSelectedTile == null && selectedTile != firstSelectedTile) {
            secondSelectedTile = selectedTile;
            isCheckingMatch = true;
            matchCheckCoroutine = StartCoroutine(CheckMatchRoutine());
        }
    }

    protected override IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);

        bool isMatch = false;
        if (firstSelectedTile != null && secondSelectedTile != null) {
            string text1 = firstSelectedTile.GetDisplayText();
            string text2 = secondSelectedTile.GetDisplayText();

            if (matchPairs != null) {
                foreach (var pair in matchPairs) {
                    if (pair == null) continue;
                    if ((pair.offerText == text1 && pair.responseText == text2) ||
                        (pair.offerText == text2 && pair.responseText == text1)) {
                        isMatch = true;
                        break;
                    }
                }
            }
        }

        if (isMatch) {
            if (firstSelectedTile != null) firstSelectedTile.SetMatched();
            if (secondSelectedTile != null) secondSelectedTile.SetMatched();
            score++;
            matchedPairsCount++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (matchPairs != null && matchedPairsCount >= matchPairs.Count) {
                GameWon();
            }
        } else {
            if (firstSelectedTile != null) firstSelectedTile.CloseTile();
            if (secondSelectedTile != null) secondSelectedTile.CloseTile();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        firstSelectedTile = null;
        secondSelectedTile = null;
        isCheckingMatch = false;
        matchCheckCoroutine = null;

        UpdateUI();
    }

    protected override void GameWon() {
        isGameActive = false;

        // Ensure title shows "Great Job!" on completion panel
        if (quizCompleteGameObject != null) {
            var textComponents = quizCompleteGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (textComponents != null && textComponents.Length > 0) {
                textComponents[0].text = "Great Job!";
            }
        }

        ShowQuizCompleteScreen();

        // Mark G02 sub-flag in Hub progression
        M3A_U6_HubProgress.MarkG02Complete();
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) topic = Masters_Topic.Game;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        // Guarantee G02 completion persistence
        M3A_U6_HubProgress.MarkG02Complete();

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnDestroy() {
        StopAllCoroutines();
    }
}
