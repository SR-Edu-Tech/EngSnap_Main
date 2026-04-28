using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Writing_LessonTwo : Masters_Lesson {


    private const string CLEAR_AND_SET_PUZZLE = "ClearAndSetPuzzle";


    [System.Serializable]
    public class ArrangeWordsPuzzle {

        public string[] wordArray;

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
    private Color wrongColor;
    [SerializeField]
    private float timeBetweenEachPuzzle;


    private int arrangeWordsPuzzleIndex;
    private ArrangeWordsPuzzle currentArrangeWordsPuzzle;
    private int currentCorrectWordIndex;
    private List<Button> currentPuzzleButtonList = new List<Button>();


    protected override void Awake() {
        base.Awake();

        ClearAndSetPuzzle();
    }

    private void ClearAndSetPuzzle() {
        if(currentPuzzleButtonList.Count > 0) {
            for (int i = currentPuzzleButtonList.Count - 1; i >= 0; i--) {
                Destroy(currentPuzzleButtonList[i].gameObject);
            }
            currentPuzzleButtonList.Clear();
        }

        if(arrangeWordsPuzzleIndex == arrangeWordsPuzzleArray.Length) {
            // Over
            nextButton.interactable = true;
            return;
        }

        currentArrangeWordsPuzzle = arrangeWordsPuzzleArray[arrangeWordsPuzzleIndex++];
        currentCorrectWordIndex = 0;

        Transform[] buttonTransformArray = (currentArrangeWordsPuzzle.wordArray.Length == 3) ? threeButtonTransformArray : 
            fourButtonTransformArray;

        HashSet<int> randomSpawnHashSet = new HashSet<int>();
        while(randomSpawnHashSet.Count != buttonTransformArray.Length) {
            int i = Random.Range(0, buttonTransformArray.Length);
            if (!randomSpawnHashSet.Contains(i)) {
                randomSpawnHashSet.Add(i);

                GameObject spawnedButtonGameObject = Instantiate(wordButtonReference.gameObject, buttonTransformArray[i]);
                spawnedButtonGameObject.transform.SetParent(buttonsParentTransform, false);
                spawnedButtonGameObject.SetActive(true);

                if (spawnedButtonGameObject.TryGetComponent(out Masters_ArrangeWordButton arrangeWordButton)) {
                    arrangeWordButton.SetButtonIndexAndWord(i, currentArrangeWordsPuzzle.wordArray[i]);

                    Button spawnedButton = spawnedButtonGameObject.GetComponent<Button>();
                    spawnedButton.onClick.AddListener(() => {
                        OnArrangeWordButtonClicked(arrangeWordButton);
                    });
                }
            }
        }
    }

    private void OnArrangeWordButtonClicked(Masters_ArrangeWordButton arrangeWordButton) {
        if(arrangeWordButton.GetButtonIndex() == currentCorrectWordIndex) {
            // Correct
            arrangeWordButton.transform.SetParent(slateWordsParentTransform, false);
            arrangeWordButton.SetTMPColor(correctColor);
            Button button = arrangeWordButton.GetComponent<Button>();
            button.interactable = false;
            currentPuzzleButtonList.Add(button);
            currentCorrectWordIndex++;

            if(currentCorrectWordIndex == currentArrangeWordsPuzzle.wordArray.Length) {
                // Load next
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                Invoke(CLEAR_AND_SET_PUZZLE, timeBetweenEachPuzzle);
            }
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            arrangeWordButton.SetTMPColor(wrongColor);
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
