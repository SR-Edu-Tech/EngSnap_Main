using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U2_L02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] bool _isViewed = false, _slided = false, _tab1Opened = false, _tab2Opened = false;
    [SerializeField] RectTransform _tab1, _tab2;
    [SerializeField] GameObject _tab1P, _tab2P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Sprite[] _spriteIndex;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;
    Coroutine _audioCoroutine, _tabChange;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        // Safety check references before fetching components
        if (_tab1 != null)
        {
            if (_tab1.TryGetComponent(out Popeffect_Junior1B pop1)) pop1.enabled = true;
            _tab1.anchoredPosition = Vector3.left * 250;
            _tab1.anchorMin = new Vector2(.5f, .5f);
            _tab1.anchorMax = new Vector2(.5f, .5f);
            if (_tab1.TryGetComponent(out Image img1)) img1.color = Color.white;
            if (_tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI text1))
            {
                text1.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            }
        }

        if (_tab2 != null)
        {
            if (_tab2.TryGetComponent(out Popeffect_Junior1B pop2)) pop2.enabled = true;
            _tab2.anchoredPosition = Vector3.right * 250;
            _tab2.anchorMin = new Vector2(.5f, .5f);
            _tab2.anchorMax = new Vector2(.5f, .5f);
            if (_tab2.TryGetComponent(out Image img2)) img2.color = Color.white;
            if (_tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI text2))
            {
                text2.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            }
        }

        if (_currentTabIndex == 0 && _tab1P != null && _tab1P.TryGetComponent(out CanvasGroup cg1)) cg1.alpha = 0;
        else if (_tab2P != null && _tab2P.TryGetComponent(out CanvasGroup cg2)) cg2.alpha = 0;

        _tab1Opened = _tab2Opened = _slided = false;
    }

    public void TabSlideUp(int index)
    {
        if (_tab1 == null || _tab2 == null) return;

        if (index == 0)
        {
            _tab1Opened = true;
            if (_tab1.TryGetComponent(out Image img1)) img1.color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            if (_tab2.TryGetComponent(out Image img2)) img2.color = Color.white;
            if (_tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI text1)) text1.color = Color.white;
            if (_tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI text2)) text2.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        else
        {
            _tab2Opened = true;
            if (_tab1.TryGetComponent(out Image img1)) img1.color = Color.white;
            if (_tab2.TryGetComponent(out Image img2)) img2.color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            if (_tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI text1)) text1.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            if (_tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI text2)) text2.color = Color.white;
        }

        if (_tab1Opened && _tab2Opened)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null) GameManager_Junior1B.Instance.Next(true);
        }

        if (_audioSource != null) _audioSource.Stop();

        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;
            
            if (_currentTabIndex == 0 && _tab1P != null) foreach (Transform child in _tab1P.transform) child.gameObject.SetActive(false);
            else if (_tab2P != null) foreach (Transform child in _tab2P.transform) child.gameObject.SetActive(false);

            if (_tabChange != null) StopCoroutine(_tabChange);
            _tabChange = StartCoroutine(OnTab());
            return;
        }

        _slided = true;
        _currentTabIndex = index;
        StartCoroutine(SlideTabUp());
    }

    IEnumerator SlideTabUp()
    {
        _slided = true;
        if (_tab1 == null || _tab2 == null || _tab1P == null || _tab2P == null) yield break;

        if (_currentTabIndex == 0) foreach (Transform child in _tab1P.transform) child.gameObject.SetActive(false);
        else foreach (Transform child in _tab2P.transform) child.gameObject.SetActive(false);

        Vector3 worldPos1 = _tab1.position;
        Vector3 worldPos2 = _tab2.position;

        _tab1.anchorMin = new Vector2(.5f, 1);
        _tab1.anchorMax = new Vector2(.5f, 1);
        _tab2.anchorMin = new Vector2(.5f, 1);
        _tab2.anchorMax = new Vector2(.5f, 1);

        _tab1.position = worldPos1;
        _tab2.position = worldPos2;

        Vector2 startPos1 = _tab1.anchoredPosition;
        Vector2 startPos2 = _tab2.anchoredPosition;
        Vector2 targetPos1 = new Vector2(startPos1.x, -250f);
        Vector2 targetPos2 = new Vector2(startPos2.x, -250f);

        CanvasGroup cg1 = _tab1P.GetComponent<CanvasGroup>();
        CanvasGroup cg2 = _tab2P.GetComponent<CanvasGroup>();

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
            _tab1.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, easedT);
            _tab2.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, easedT);
            if (cg1 != null) cg1.alpha = easedT;
            if (cg2 != null) cg2.alpha = easedT;

            yield return null;
        }

        _tab1.anchoredPosition = targetPos1;
        _tab2.anchoredPosition = targetPos2;

        if (cg1 != null) cg1.alpha = 1;
        if (cg2 != null) cg2.alpha = 1;

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        
        if (_currentTabIndex == 0)
        {
            if (_tab1P == null || _tab1P.transform.childCount == 0) yield break;
            
            Transform lastChild = _tab1P.transform.GetChild(_tab1P.transform.childCount - 1);
            lastChild.gameObject.SetActive(false);
            lastChild.gameObject.SetActive(true);

            yield return new WaitForSeconds(1);

            for (int i = 0; i < _tab1P.transform.childCount - 1; i++)
            {
                Transform child = _tab1P.transform.GetChild(i);
                child.gameObject.SetActive(true);
                if (child.TryGetComponent(out Button btn)) btn.interactable = false;

                yield return new WaitForSeconds(.25f);
            }
            yield return new WaitForSeconds(.25f);

            for (int i = 0; i < _tab1P.transform.childCount - 1; i++)
            {
                if (lastChild.TryGetComponent(out Popeffect_Junior1B pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
                
                Transform child = _tab1P.transform.GetChild(i);
                if (child.TryGetComponent(out Button btn))
                {
                    btn.onClick.Invoke();
                }

                // 💡 FIX: Keep clip check safe from throwing errors
                if (_audioSource != null && _audioSource.clip != null)
                {
                    yield return new WaitForSeconds(_audioSource.clip.length);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            for (int i = 0; i < _tab1P.transform.childCount - 1; i++)
            {
                if (_tab1P.transform.GetChild(i).TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
        else
        {
            if (_tab2P == null || _tab2P.transform.childCount == 0 || _spriteIndex == null || _spriteIndex.Length == 0) yield break;
            int _currentSpriteIndex = 0;

            Transform lastChild = _tab2P.transform.GetChild(_tab2P.transform.childCount - 1);
            if (lastChild.TryGetComponent(out Image lastImg)) lastImg.sprite = _spriteIndex[_currentSpriteIndex];
            
            lastChild.gameObject.SetActive(false);
            lastChild.gameObject.SetActive(true);

            yield return new WaitForSeconds(1);

            for (int i = 0; i < _tab2P.transform.childCount - 1; i++)
            {
                Transform child = _tab2P.transform.GetChild(i);
                child.gameObject.SetActive(true);
                if (child.TryGetComponent(out Button btn)) btn.interactable = false;

                yield return new WaitForSeconds(.25f);
            }
            yield return new WaitForSeconds(.25f);

            for (int i = 0; i < _tab2P.transform.childCount - 1; i++)
            {
                if (lastChild.TryGetComponent(out Popeffect_Junior1B pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
                
                if (_currentSpriteIndex < _spriteIndex.Length && lastChild.TryGetComponent(out Image img)) 
                {
                    img.sprite = _spriteIndex[_currentSpriteIndex];
                }
                
                Transform child = _tab2P.transform.GetChild(i);
                if (child.TryGetComponent(out Button btn))
                {
                    btn.onClick.Invoke();
                }
                _currentSpriteIndex++;

                // 💡 FIX: Avoid reading length off empty/null sound clips
                if (_audioSource != null && _audioSource.clip != null)
                {
                    yield return new WaitForSeconds(_audioSource.clip.length);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            for (int i = 0; i < _tab2P.transform.childCount - 1; i++)
            {
                if (_tab2P.transform.GetChild(i).TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    IEnumerator PlayAudioIndex()
    {
        if (_audioSource == null || _audioClips == null || _currentAudioClipIndex >= _audioClips.Length || _audioClips[_currentAudioClipIndex] == null) yield break;

        _audioSource.clip = _audioClips[_currentAudioClipIndex];
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}