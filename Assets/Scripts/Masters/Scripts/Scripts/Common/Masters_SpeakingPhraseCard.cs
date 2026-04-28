using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_SpeakingPhraseCard : MonoBehaviour {


    [SerializeField]
    private TextMeshProUGUI statementTMP;


    public void SetText(string text) {
        statementTMP.text = text;
    }

    
}
