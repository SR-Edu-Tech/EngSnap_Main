using UnityEngine;
using UnityEngine.UI;

public class Masters_UniversalSortBin : MonoBehaviour {

    [SerializeField]
    private Button button;
    [SerializeField]
    private RectTransform phraseTargetPointRectTransform;
    [Tooltip("The ID used to match with the phrase card. e.g., 0 for Transitive, 1 for Intransitive.")]
    [SerializeField]
    private int sortId;

    public Button GetButton() {
        if (button == null) button = GetComponent<Button>();
        return button;
    }

    public RectTransform GetPhraseTargetPointRectTransform() {
        if (phraseTargetPointRectTransform == null) phraseTargetPointRectTransform = GetComponent<RectTransform>();
        return phraseTargetPointRectTransform;
    }

    public int GetSortId() {
        return sortId;
    }

    public void SetSortId(int id) {
        sortId = id;
    }
}
