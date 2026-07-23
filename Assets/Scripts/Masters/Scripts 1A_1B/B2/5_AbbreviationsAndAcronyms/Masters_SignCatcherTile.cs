using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Button))]
public class Masters_SignCatcherTile : MonoBehaviour {
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI tileTextTMP;
    [SerializeField] private Image tileBackgroundImage;
    [SerializeField] private Button tileButton;
    
    [Header("Visuals")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    
    private RectTransform rectTransform;
    private Masters_AbbreviationsAndAcronyms_Game_LessonOne gameManager;
    
    public int pairId { get; private set; }
    public bool isAbbreviation { get; private set; }
    
    private float fallSpeed = 100f;
    private float dropThresholdY = -800f; // Adjust based on canvas size
    
    private bool isLocked = false;
    private bool isSelected = false;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        if (tileButton == null) tileButton = GetComponent<Button>();
        
        tileButton.onClick.AddListener(OnTileClicked);
    }

    public void Setup(string text, int pairId, bool isAbbreviation, float speed, float dropLimit, Masters_AbbreviationsAndAcronyms_Game_LessonOne manager) {
        if (tileTextTMP != null) tileTextTMP.text = text;
        this.pairId = pairId;
        this.isAbbreviation = isAbbreviation;
        this.fallSpeed = speed;
        this.dropThresholdY = dropLimit;
        this.gameManager = manager;
        
        SetSelectedState(false);
    }

    private void Update() {
        if (isLocked) return;
        if (gameManager != null && !gameManager.IsGameActive()) return;

        // Fall downward
        rectTransform.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;

        // Check if it hit the floor
        if (rectTransform.anchoredPosition.y < dropThresholdY) {
            isLocked = true;
            gameManager.OnTileDropped(this);
            Destroy(gameObject);
        }
    }

    private void OnTileClicked() {
        if (isLocked) return;
        if (gameManager != null && !gameManager.IsGameActive()) return;
        
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        gameManager.OnTileClicked(this);
    }

    public void SetSelectedState(bool selected) {
        isSelected = selected;
        if (tileBackgroundImage != null) {
            tileBackgroundImage.color = isSelected ? selectedColor : defaultColor;
        }
    }
    
    public void LockAndDestroy() {
        isLocked = true;
        // Optionally add DOTween animations here later, handled by manager
        Destroy(gameObject);
    }
}
