using System.Collections;
using TMPro;
using UnityEngine;

public class TextPopEffect_Junior1A : MonoBehaviour
{
    [SerializeField] TMP_Text _tmpText;
    [SerializeField] float popDuration = 1.5f, popAmplitude = 0.5f, frequency = 3f, stagger = 0.03f;

    void OnEnable() => StartCoroutine(TextPop());
    IEnumerator TextPop()
    {
        _tmpText = GetComponent<TMP_Text>();
        _tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = _tmpText.textInfo;

        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        float elapsed = 0f;

        int initialCharCount = textInfo.characterCount;
        float expectedTotalTime = (initialCharCount * stagger) + Mathf.Max(0.5f, 1f / frequency);
        float totalDuration = Mathf.Max(popDuration, expectedTotalTime);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            textInfo = _tmpText.textInfo;

            int characterCount = textInfo.characterCount;

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                Vector3 charMid = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2f;

                float letterDelay = i * stagger;
                float localTime = elapsed - letterDelay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterPopDuration = Mathf.Max(0.1f, 1f / frequency);
                    float t = Mathf.Clamp01(localTime / letterPopDuration);

                    float overshoot = 1.70158f * (1f + popAmplitude);
                    float c3_dynamic = overshoot + 1f;

                    scale = 1f + c3_dynamic * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }
                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[materialIndex].vertices[vertexIndex + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertexIndex + v] = charMid + offset * scale;
                }
            }
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                _tmpText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }
            yield return null;
        }
        textInfo = _tmpText.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            _tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
