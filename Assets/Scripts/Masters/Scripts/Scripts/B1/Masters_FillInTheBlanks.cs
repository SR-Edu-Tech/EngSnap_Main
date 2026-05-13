using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_FillInTheBlanks : MonoBehaviour {



    [System.Serializable]
    public struct BlanksAndWords {

        public Button[] blankButtonArray;
        public Button[] wordButtonArray;

    }


    [SerializeField]
    private GameObject[] statementGameObjectArray;
    [SerializeField]
    private GameObject[] wordsGameObjectArray;
    [SerializeField]
    private BlanksAndWords[] blanksAndWordsArray;


    private BlanksAndWords currentBlanksAndWords;


    public GameObject[] GetStatementGameObjectArray() {
        return statementGameObjectArray;
    }

    public GameObject[] GetWordsGameObjectArray() {
        return wordsGameObjectArray;
    }

    public BlanksAndWords[] GetBlanksAndWordsArray() {
        return blanksAndWordsArray;
    }

    public BlanksAndWords GetCurrentBlanksAndWords() {
        return currentBlanksAndWords;
    }

    public void SetCurrentBlanksAndWords(BlanksAndWords blanksAndWords) {
        currentBlanksAndWords = blanksAndWords;
    }


}
