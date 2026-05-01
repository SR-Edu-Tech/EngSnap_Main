using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Writing_LessonTwo : Masters_Lesson {


    private const string CLEAR_AND_SET_PUZZLE = "ClearAndSetPuzzle";


    [System.Serializable]
    public class ArrangeWordsPuzzle {

        public string[] buttonTMPArray;
        public AudioClip sentenceAudioClip;

    }


    [SerializeField]
    private ArrangeWordsPuzzle[] arrangeWordsPuzzleArray;
    [SerializeField]
    private Transform[] threeButtonTransformArray;
    [SerializeField]
    private Transform[] fourButtonTransformArray;
    [SerializeField]
    private Button wordButtonReference;
    [SerializeField]
    private Transform buttonsParentTransform;
    [SerializeField]
    private Transform slateWordsParentTransform;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color incorrectColor;
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private float timeBetweenEachPuzzle;
    [SerializeField]
    private Button checkButton;
    [SerializeField]
    private float twoWordSpacing, threeWordSpacing, fourWordSpacing;
    [SerializeField]
    private float timeBetweenEachAnimation, animationTime;
    [SerializeField]
    private RectTransform slateRectTransform;
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private Transform completedPanelRectTransform;


    private int arrangeWordsPuzzleIndex;
    private ArrangeWordsPuzzle currentArrangeWordsPuzzle;
    private int currentCorrectWordIndex;
    private List<Button> currentPuzzleButtonList = new List<Button>();
    private bool canClickCheck;


    protected override void Awake() {
        base.Awake();

        ClearAndSetPuzzle();
        checkButton.onClick.AddListener(OnCheckButtonClicked);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    private void OnRetryButtonClicked() {
        arrangeWordsPuzzleIndex = 0;
        completedPanelRectTransform.DOScale(Vector3.zero, animationTime).SetEase(Ease.OutExpo).OnComplete(() => {
            slateRectTransform.gameObject.SetActive(true);
            ClearAndSetPuzzle();
        });
    }

    private void OnCheckButtonClicked() {
        Masters_ArrangeWordButton[] arrangeWordButtonArray = slateWordsParentTransform.transform.
            GetComponentsInChildren<Masters_ArrangeWordButton>();

        if(arrangeWordButtonArray.Length == 0) {
            return;
        }

        int totalToProceed = currentArrangeWordsPuzzle.buttonTMPArray.Length;
        int currentCorrectAmount = 0;

        for(int i = 0; i < currentArrangeWordsPuzzle.buttonTMPArray.Length; i++) {
            if (arrangeWordButtonArray[i].GetButtonString() == currentArrangeWordsPuzzle.buttonTMPArray[i]) {
                // Correct
                currentCorrectAmount++;
                arrangeWordButtonArray[i].SetButtonTextColor(correctColor);
            } else {
                // Incorrect
                arrangeWordButtonArray[i].SetButtonTextColor(incorrectColor);
            }
        }

        if(currentCorrectAmount == totalToProceed) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            Masters_AudioManager.Instance.PlayVoiceOver(currentArrangeWordsPuzzle.sentenceAudioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(ClearAndSetNewPuzzle));
            canClickCheck = false;
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        checkButton.gameObject.SetActive(false);
    }

    private void ClearAndSetNewPuzzle() {
        Transform[] slateGameObjectArray = slateWordsParentTransform.GetComponentsInChildren<Transform>();

        // Clear all existing words in slate
        for (int i = slateGameObjectArray.Length - 1; i > 0; i--) {
            Destroy(slateGameObjectArray[i].gameObject);
        }

        ClearAndSetPuzzle();
    }

    private void ClearAndSetPuzzle() {
        if (arrangeWordsPuzzleIndex == arrangeWordsPuzzleArray.Length) {
            // Over
            slateRectTransform.DOScale(Vector3.zero, animationTime).SetEase(Ease.OutExpo).OnComplete(() => {
                slateRectTransform.gameObject.SetActive(false);
                completedPanelRectTransform.gameObject.SetActive(true);
            });
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        currentArrangeWordsPuzzle = arrangeWordsPuzzleArray[arrangeWordsPuzzleIndex++];
        currentCorrectWordIndex = 0;

        Transform[] buttonTransformArray;
        HorizontalLayoutGroup wordsHorizontalLayoutGroup = buttonsParentTransform.GetComponent<HorizontalLayoutGroup>();

        if (currentArrangeWordsPuzzle.buttonTMPArray.Length == 3) {
            // Three words
            wordsHorizontalLayoutGroup.spacing = threeWordSpacing;
            buttonTransformArray = threeButtonTransformArray;
        } else {
            // Four words
            wordsHorizontalLayoutGroup.spacing = fourWordSpacing;
            buttonTransformArray = fourButtonTransformArray;
        }

        StartCoroutine(SpawnButtonCoroutine(buttonTransformArray));
    }

    private IEnumerator SpawnButtonCoroutine(Transform[] buttonTransformArray) {
        HashSet<int> randomSpawnHashSet = new HashSet<int>();
        int buttonNumber = 0;
        while (randomSpawnHashSet.Count != buttonTransformArray.Length) {
            int i = Random.Range(0, buttonTransformArray.Length);
            if (!randomSpawnHashSet.Contains(i)) {
                randomSpawnHashSet.Add(i);
                buttonNumber++;

                HorizontalLayoutGroup wordsHorizontalLayoutGroup = buttonsParentTransform.GetComponent<HorizontalLayoutGroup>();
                switch (buttonNumber) {
                    case 2:
                        wordsHorizontalLayoutGroup.spacing = twoWordSpacing;
                        break;
                    case 3:
                        wordsHorizontalLayoutGroup.spacing = threeWordSpacing;
                        break;
                    case 4:
                        wordsHorizontalLayoutGroup.spacing = fourWordSpacing;
                        break;
                }

                yield return new WaitForSeconds(buttonNumber * timeBetweenEachAnimation);

                GameObject spawnedButtonGameObject = Instantiate(wordButtonReference.gameObject, buttonTransformArray[i]);
                spawnedButtonGameObject.transform.SetParent(buttonsParentTransform, false);
                spawnedButtonGameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(spawnedButtonGameObject.GetComponent<RectTransform>());

                if (spawnedButtonGameObject.TryGetComponent(out Masters_ArrangeWordButton arrangeWordButton)) {
                    arrangeWordButton.SetButtonTextAndStringTMP(currentArrangeWordsPuzzle.buttonTMPArray[i]);

                    Button spawnedButton = spawnedButtonGameObject.GetComponent<Button>();
                    spawnedButton.onClick.AddListener(() => {
                        OnArrangeWordButtonClicked(arrangeWordButton);
                    });
                }
            }
        }
        canClickCheck = true;
    }

    private void OnArrangeWordButtonClicked(Masters_ArrangeWordButton arrangeWordButton) {
        HorizontalLayoutGroup slateWordsHorizontalLayoutGroup = slateWordsParentTransform.
                GetComponent<HorizontalLayoutGroup>();
        HorizontalLayoutGroup wordsHorizontalLayoutGroup = buttonsParentTransform.GetComponent<HorizontalLayoutGroup>();
        if (arrangeWordButton.GetIsInBox() == false) {
            // Is Down

            if (slateWordsParentTransform.childCount == 1) {
                // Two word spacing
                slateWordsHorizontalLayoutGroup.spacing = twoWordSpacing;
            } else if(slateWordsParentTransform.childCount == 2) {
                // Three word spacing
                slateWordsHorizontalLayoutGroup.spacing = threeWordSpacing;
            } else if(slateWordsParentTransform.childCount == 3) {
                // Four word spacing
                slateWordsHorizontalLayoutGroup.spacing = fourWordSpacing;
            }

            if (buttonsParentTransform.childCount == 3) {
                // Two word spacing
                wordsHorizontalLayoutGroup.spacing = twoWordSpacing;
            } else if (buttonsParentTransform.childCount == 4) {
                // Three word spacing
                wordsHorizontalLayoutGroup.spacing = threeWordSpacing;
            }

            arrangeWordButton.transform.SetParent(slateWordsParentTransform, false);
            arrangeWordButton.SetIsInBox(true);

            if(buttonsParentTransform.childCount == 0 && canClickCheck) {
                // Enable check button
                checkButton.gameObject.SetActive(true);
            }
        } else {
            // Is Up

            if (buttonsParentTransform.childCount == 1) {
                // Two word spacing
                wordsHorizontalLayoutGroup.spacing = twoWordSpacing;
            } else if (buttonsParentTransform.childCount == 2) {
                // Three word spacing
                wordsHorizontalLayoutGroup.spacing = threeWordSpacing;
            } else if (buttonsParentTransform.childCount == 3) {
                // Four word spacing
                wordsHorizontalLayoutGroup.spacing = fourWordSpacing;
            }

            if (slateWordsParentTransform.childCount == 3) {
                // Two word spacing
                slateWordsHorizontalLayoutGroup.spacing = twoWordSpacing;
            } else if (slateWordsParentTransform.childCount == 4) {
                // Three word spacing
                slateWordsHorizontalLayoutGroup.spacing = threeWordSpacing;
            }

            checkButton.gameObject.SetActive(false);
            arrangeWordButton.SetButtonTextColor(defaultColor);
            arrangeWordButton.transform.SetParent(buttonsParentTransform, false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(arrangeWordButton.GetComponent<RectTransform>());
            arrangeWordButton.SetIsInBox(false);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }


}
