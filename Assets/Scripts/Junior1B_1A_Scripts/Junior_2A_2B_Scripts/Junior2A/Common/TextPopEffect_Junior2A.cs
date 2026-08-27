using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Junior2A
{
    public class TextPopEffect_Junior2A : MonoBehaviour
    {
        [SerializeField] TMP_Text _tmpText;
        [SerializeField] float popDuration = 1.5f, popAmplitude = 0.5f, frequency = 3f, stagger = 0.03f;

        [Tooltip("Multiplier for the overall speed. 2 = twice as fast, 0.5 = half speed.")]
        [SerializeField] public float speedMultiplier = 2.0f;

        void OnEnable() => StartCoroutine(TextPop());

        IEnumerator TextPop()
        {
            _tmpText = GetComponent<TMP_Text>();
            if (_tmpText == null) yield break;

            _tmpText.ForceMeshUpdate();
            yield return new WaitForEndOfFrame();

            float elapsed = 0f;

            float scaledStagger = stagger / speedMultiplier;
            float scaledFrequency = frequency * speedMultiplier;

            int initialCharCount = _tmpText.textInfo.characterCount;
            float expectedTotalTime = (initialCharCount * scaledStagger) + Mathf.Max(0.5f / speedMultiplier, 1f / scaledFrequency);
            float totalDuration = Mathf.Max(popDuration / speedMultiplier, expectedTotalTime);

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                _tmpText.ForceMeshUpdate();
                TMP_TextInfo textInfo = _tmpText.textInfo;
                int characterCount = textInfo.characterCount;

                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    int materialIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                    if (vertexIndex + 3 >= vertices.Length) continue;

                    Vector3 charMid = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2f;

                    float letterDelay = i * scaledStagger;
                    float localTime = elapsed - letterDelay;

                    float scale = 0f;
                    if (localTime > 0f)
                    {
                        float letterPopDuration = Mathf.Max(0.1f / speedMultiplier, 1f / scaledFrequency);
                        float t = Mathf.Clamp01(localTime / letterPopDuration);

                        float overshoot = 1.70158f * (1f + popAmplitude);
                        float c3_dynamic = overshoot + 1f;

                        scale = 1f + c3_dynamic * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                    }

                    for (int v = 0; v < 4; v++)
                    {
                        Vector3 orig = vertices[vertexIndex + v];
                        Vector3 offset = orig - charMid;
                        vertices[vertexIndex + v] = charMid + (offset * scale);
                    }
                }

                for (int m = 0; m < textInfo.meshInfo.Length; m++)
                {
                    if (textInfo.meshInfo[m].mesh == null) continue;
                    textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                    _tmpText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
                }

                yield return null;
            }

            _tmpText.ForceMeshUpdate();
        }
    }
}