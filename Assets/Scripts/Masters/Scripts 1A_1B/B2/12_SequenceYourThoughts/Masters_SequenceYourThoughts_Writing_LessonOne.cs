using UnityEngine;

/// <summary>
/// Writing Lesson 1 for Unit 12 Sequence Your Thoughts.
/// Implements typed connector word fill-in-the-blanks with hint showing a random accepted answer.
/// </summary>
public class Masters_SequenceYourThoughts_Writing_LessonOne : Masters_IsThereADifference_Writing_LessonOne {

    protected override void ShowHint() {
        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintTMP != null && questions != null && currentQuestionIndex < questions.Length) {
            QuestionData currentQ = questions[currentQuestionIndex];
            if (currentQ.acceptedAnswers != null && currentQ.acceptedAnswers.Length > 0) {
                int randomIdx = UnityEngine.Random.Range(0, currentQ.acceptedAnswers.Length);
                hintTMP.text = $"Hint: {currentQ.acceptedAnswers[randomIdx]}";
            } else {
                hintTMP.text = $"Hint: {currentQ.displayAnswer}";
            }
        }
    }
}
