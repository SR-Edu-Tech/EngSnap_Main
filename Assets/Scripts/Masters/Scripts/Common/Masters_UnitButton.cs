using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Masters_UnitButton : MonoBehaviour {


    [SerializeField] 
    private Masters_Unit unit;
    [SerializeField]
    private RectTransform parentRectTransform;
    [SerializeField]
    private float popUpAnimationTime, timeBetweenEachAnimation;
    [SerializeField]
    private int order;
    [SerializeField] 
    TMP_Text _tmpText;
    [SerializeField] 
    float popDuration = 1.75f, popAmplitude = 0.75f, frequency = 4f, stagger = 0.05f;


    private Button button;
    private int maxVisibleCharacters;


    private void Awake() {
        button = this.GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
        maxVisibleCharacters = _tmpText.maxVisibleCharacters;
    }

    private void OnEnable() {
        StartCoroutine(StartingAnimationCoroutine());
        _tmpText.maxVisibleCharacters = 0;
        _tmpText.ForceMeshUpdate();
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    private void OnButtonClicked() {
        Masters_LevelManager.Instance.OnUnitButtonClicked(unit);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private IEnumerator StartingAnimationCoroutine() {
        parentRectTransform.localScale = Vector3.zero;
        yield return new WaitForSeconds(timeBetweenEachAnimation * order);
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(this.transform.position);

        bool isVisible =
            viewportPos.z > 0 &&                // in front of camera
            viewportPos.x > 0 && viewportPos.x < 1 &&
            viewportPos.y > 0 && viewportPos.y < 1;

        if (isVisible) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }
        parentRectTransform.DOScale(Vector3.one, popUpAnimationTime).SetEase(Ease.OutExpo).OnComplete(() => {
            if (_tmpText.isActiveAndEnabled) {
                StartCoroutine(TextPop());
            }
        });
    }

    private IEnumerator TextPop() {
        _tmpText.maxVisibleCharacters = maxVisibleCharacters;
        _tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = _tmpText.textInfo;

        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        float elapsed = 0f;

        int initialCharCount = textInfo.characterCount;
        float expectedTotalTime = (initialCharCount * stagger) + Mathf.Max(0.5f, 1f / frequency);
        float totalDuration = Mathf.Max(popDuration, expectedTotalTime);

        while (elapsed < totalDuration) {
            elapsed += Time.deltaTime;

            textInfo = _tmpText.textInfo;

            int characterCount = textInfo.characterCount;

            for (int i = 0; i < characterCount; i++) {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                Vector3 charMid = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2f;

                float letterDelay = i * stagger;
                float localTime = elapsed - letterDelay;

                float scale = 0f;
                if (localTime > 0f) {
                    float letterPopDuration = Mathf.Max(0.1f, 1f / frequency);
                    float t = Mathf.Clamp01(localTime / letterPopDuration);

                    float overshoot = 1.70158f * (1f + popAmplitude);
                    float c3_dynamic = overshoot + 1f;

                    scale = 1f + c3_dynamic * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }
                for (int v = 0; v < 4; v++) {
                    Vector3 orig = cachedMeshInfo[materialIndex].vertices[vertexIndex + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertexIndex + v] = charMid + offset * scale;
                }
            }
            for (int m = 0; m < textInfo.meshInfo.Length; m++) {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                _tmpText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }
            yield return null;
        }
        textInfo = _tmpText.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++) {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            _tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }


}
