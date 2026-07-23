using UnityEngine;
using DG.Tweening;

/// <summary>
/// Writing Lesson 1 for Unit 15 Presentation Pointers.
/// Implements single-word topic fill-in-the-blank validated against the word dictionary.
/// </summary>
public class Masters_PresentationPointers_Writing_LessonOne : Masters_SequenceYourThoughts_Writing_LessonOne {

    protected override void OnCheckButtonClicked() {
        if (!canCheck || inputField == null) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        if (Masters_SentenceValidator.IsProfanity(input)) {
            if (hintTMP != null && hintPanel != null) {
                hintPanel.SetActive(true);
                hintTMP.text = "Inappropriate or vulgar language is not permitted!";
            }
            WrongAnswer();
            return;
        }

        string[] tokens = input.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        bool isSingleWord = (tokens.Length == 1);

        bool isCorrect = false;
        if (isSingleWord) {
            string cleanToken = tokens[0].Trim();
            if (Masters_SentenceValidator.IsValidSingleWord(cleanToken)) {
                isCorrect = true;
            }
        }

        // Fallback: check explicitly provided acceptedAnswers
        if (!isCorrect && questions != null && currentQuestionIndex < questions.Length) {
            QuestionData currentQ = questions[currentQuestionIndex];
            string cleanInput = input.ToLowerInvariant().Replace(".", "").Replace("!", "").Replace("?", "").Trim();
            if (currentQ.acceptedAnswers != null) {
                foreach (string ans in currentQ.acceptedAnswers) {
                    if (string.IsNullOrEmpty(ans)) continue;
                    if (cleanInput == ans.ToLowerInvariant().Trim()) {
                        isCorrect = true;
                        break;
                    }
                }
            }
        }

        if (!isCorrect) {
            if (!isSingleWord && hintTMP != null && hintPanel != null) {
                hintPanel.SetActive(true);
                hintTMP.text = "Please enter exactly one correctly spelled topic word!";
            }
            WrongAnswer();
            return;
        }

        CorrectAnswer();
    }
}
