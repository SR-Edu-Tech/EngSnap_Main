using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MatchCard : MonoBehaviour {


    [SerializeField]
    private int cardIndex;
    [SerializeField]
    private Color completeColor;
    [SerializeField]
    private GameObject tickGameObject;


    private Image image;
    private Button button;


    private void Awake() {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    public Button GetButton() {
        return button;
    }

    public int GetCardIndex() {
        return cardIndex;
    }

    public void CompleteCard() {
        image.color = completeColor;
        tickGameObject.SetActive(true);
    }

    
}
