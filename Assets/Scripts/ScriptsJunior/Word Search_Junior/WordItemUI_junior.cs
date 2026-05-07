using UnityEngine;
using UnityEngine.UI;

// WordItemUI — Displays a word in the word list.
// On found: word turns green + bold, and a strikethrough line activates.
//
// SETUP: In your WordItem prefab, add a child GameObject named "StrikeThrough"
// with an Image component (thin horizontal white/gray bar). Start it disabled.
public class WordItemUI_junior : MonoBehaviour
{
    public Text wordText;

    // Optional: assign in Inspector, or leave null (auto-found by name).
    public GameObject strikeThrough;

    private string word;
    private bool found;

    void Awake()
    {
        // Auto-find StrikeThrough child if not assigned in Inspector
        if (strikeThrough == null)
        {
            Transform st = transform.Find("StrikeThrough");
            if (st != null) strikeThrough = st.gameObject;
        }

        // Make sure strikethrough starts hidden
        if (strikeThrough != null)
            strikeThrough.SetActive(false);
    }

    public void Init(string w)
    {
        word  = w;
        found = false;
        wordText.text      = w;
        //wordText.color     = Color.white; // default color — change to match your UI
        wordText.fontStyle = FontStyle.Normal;

        if (strikeThrough != null)
            strikeThrough.SetActive(false);
    }

    public void MarkFound()
    {
        if (found) return;
        found = true;

        wordText.color     = Color.green;
        wordText.fontStyle = FontStyle.Bold;

        // Activate strikethrough line
        if (strikeThrough != null)
            strikeThrough.SetActive(true);
    }

    public bool  IsFound()  => found;
    public string GetWord() => word;
}