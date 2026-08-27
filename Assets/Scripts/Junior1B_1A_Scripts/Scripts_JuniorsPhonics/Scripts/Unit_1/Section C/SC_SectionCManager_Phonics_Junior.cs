using UnityEngine;

public class SC_SectionManager_Phonics_Junior : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject learnPanel;
    [SerializeField] private GameObject quizPanel;

    private void Start()
    {
        learnPanel.SetActive(true);
        quizPanel.SetActive(false);
    }

    public void OpenQuiz()
    {
        learnPanel.SetActive(false);
        quizPanel.SetActive(true);
    }
}