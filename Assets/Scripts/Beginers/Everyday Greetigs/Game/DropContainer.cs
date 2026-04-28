using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropContainer : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum Type { Greeting, Response }
    public Type containerType;

    public Image background;
    public Color normalColor    = Color.white;
    public Color highlightColor = new Color(0.8f, 1f, 0.8f);

    [Header("Audio")]
    [Tooltip("Sound played when a word is successfully dropped into this container.")]
    public AudioClip dropSound;
    private static AudioSource _sharedSource;

    void Awake()
    {
        if (_sharedSource == null)
        {
            var go = new GameObject("DropContainer_SharedAudioSource");
            DontDestroyOnLoad(go);
            _sharedSource = go.AddComponent<AudioSource>();
            _sharedSource.playOnAwake = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (background != null)
            background.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null)
            background.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var draggable = eventData.pointerDrag.GetComponent<DraggableWord>();

        if (draggable == null || draggable.isDropped) return;

        // Parent to this container
        draggable.transform.SetParent(transform, false);
        draggable.transform.localScale    = Vector3.one;
        draggable.transform.localRotation = Quaternion.identity;

        // Lock so it can't be dragged again
        draggable.LockInPlace();

        // Play drop sound
        if (dropSound != null && _sharedSource != null)
            _sharedSource.PlayOneShot(dropSound);

        // Re-stack all children neatly
        ArrangeChildren();

        // Reset highlight
        if (background != null)
            background.color = normalColor;

        StartCoroutine(PopEffect(draggable.transform));

        // Tell the game controller a word was dropped (so it can check submit visibility)
        var controller = FindObjectOfType<DragDropGameController>();
        if (controller != null)
            controller.OnWordDropped();
    }

    void ArrangeChildren()
    {
        float spacingY = 80f;
        int count = transform.childCount;

        for (int i = 0; i < count; i++)
        {
            RectTransform child = transform.GetChild(i).GetComponent<RectTransform>();
            if (child == null) continue;

            child.anchorMin        = new Vector2(0.5f, 1f);
            child.anchorMax        = new Vector2(0.5f, 1f);
            child.pivot            = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = new Vector2(0, -40f - i * spacingY);
        }
    }

    private System.Collections.IEnumerator PopEffect(Transform target)
    {
        float time     = 0f;
        float duration = 0.2f;

        while (time < duration)
        {
            float scale = Mathf.Lerp(1f, 1.15f, time / duration);
            target.localScale = Vector3.one * scale;
            time += Time.deltaTime;
            yield return null;
        }

        // Bounce back
        time = 0f;
        while (time < duration)
        {
            float scale = Mathf.Lerp(1.15f, 1f, time / duration);
            target.localScale = Vector3.one * scale;
            time += Time.deltaTime;
            yield return null;
        }

        target.localScale = Vector3.one;
    }
}