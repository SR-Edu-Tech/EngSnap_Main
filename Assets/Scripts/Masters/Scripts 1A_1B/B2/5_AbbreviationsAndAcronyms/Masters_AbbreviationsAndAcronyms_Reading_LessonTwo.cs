using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_AbbreviationsAndAcronyms_Reading_LessonTwo : Masters_Lesson {
    
    [System.Serializable]
    public class AbbreviationScene {
        public GameObject sceneContainer; 
        [Header("MCQ Data")]
        public string[] mcqOptions; 
        public int correctOptionIndex;
    }

    [Header("Lesson Settings")]
    [SerializeField] private AbbreviationScene[] abbreviationScenes;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private float animationSpeed = 0.5f;
    [SerializeField] private float timeBetweenScenes = 1.0f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentSceneIndex = 0;
    private bool canSelectOption = false;

    protected override void Awake() {
        base.Awake();
        for (int i = 0; i < optionButtons.Length; i++) {
            int index = i;
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
    }

    protected override void Start() {
        base.Start();
        if (optionsContainer != null) optionsContainer.SetActive(false);
        
        foreach (var scene in abbreviationScenes) {
            if (scene.sceneContainer != null) scene.sceneContainer.SetActive(false);
        }

        if (abbreviationScenes != null && abbreviationScenes.Length > 0) {
            LoadScene(0);
        }
    }

    private void LoadScene(int index) {
        if (index >= abbreviationScenes.Length) {
            // Lesson Over
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        currentSceneIndex = index;
        AbbreviationScene currentScene = abbreviationScenes[index];

        if (progressionTMP != null) {
            progressionTMP.text = $"{currentSceneIndex + 1}/{abbreviationScenes.Length}";
        }
        
        if (currentScene.sceneContainer != null) {
            currentScene.sceneContainer.SetActive(true);
            
            // Find the tap button inside the container and assign it
            Button tapButton = currentScene.sceneContainer.GetComponentInChildren<Button>();
            if (tapButton != null) {
                tapButton.onClick.RemoveAllListeners();
                tapButton.onClick.AddListener(OnTapTargetClicked);
                tapButton.interactable = true;
                
                // Pulse animation
                RectTransform tapRect = tapButton.GetComponent<RectTransform>();
                tapRect.localScale = Vector3.one;
                tapRect.DOScale(1.05f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }
    }

    private void OnTapTargetClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        AbbreviationScene currentScene = abbreviationScenes[currentSceneIndex];
        
        Button tapButton = currentScene.sceneContainer.GetComponentInChildren<Button>();
        if (tapButton != null) {
            tapButton.interactable = false;
            RectTransform tapRect = tapButton.GetComponent<RectTransform>();
            tapRect.DOKill();
            tapRect.localScale = Vector3.one;
        }

        if (optionsContainer != null) {
            optionsContainer.SetActive(true);
            optionsContainer.transform.localScale = Vector3.zero;
            optionsContainer.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }

        for (int i = 0; i < optionButtons.Length; i++) {
            optionButtons[i].image.color = defaultColor;
            if (i < currentScene.mcqOptions.Length) {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = currentScene.mcqOptions[i];
            } else {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        canSelectOption = true;
    }

    private void OnOptionSelected(int index) {
        if (!canSelectOption) return;

        AbbreviationScene currentScene = abbreviationScenes[currentSceneIndex];

        if (index == currentScene.correctOptionIndex) {
            canSelectOption = false;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            optionButtons[index].image.color = correctColor;
            
            StartCoroutine(TransitionToNextSceneCoroutine());
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            optionButtons[index].image.color = wrongColor;
            // Let them try again
        }
    }

    private IEnumerator TransitionToNextSceneCoroutine() {
        yield return new WaitForSeconds(timeBetweenScenes);
        
        if (optionsContainer != null) {
            optionsContainer.transform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                optionsContainer.SetActive(false);
            });
        }
        
        yield return new WaitForSeconds(animationSpeed);

        if (abbreviationScenes[currentSceneIndex].sceneContainer != null) {
            abbreviationScenes[currentSceneIndex].sceneContainer.SetActive(false);
        }

        LoadScene(currentSceneIndex + 1);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
