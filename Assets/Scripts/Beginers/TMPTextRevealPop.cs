using System.Collections;
using TMPro;
using UnityEngine;

public class TMPTextRevealPop : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public float revealSpeed = 0.05f;
    public float popScale = 1.3f;
    public float popDuration = 0.15f;

    private string fullText;

    void Start()
    {
        fullText = textComponent.text;
        textComponent.text = "";
        StartCoroutine(AnimateText());
    }

    IEnumerator AnimateText()
    {
        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];
            StartCoroutine(PopCharacter(i));
            yield return new WaitForSeconds(revealSpeed);
        }
    }

    IEnumerator PopCharacter(int charIndex)
    {
        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;
        if (charIndex >= textInfo.characterCount) yield break;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) yield break;

        int vertexIndex = charInfo.vertexIndex;
        int materialIndex = charInfo.materialReferenceIndex;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        Vector3 center = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2;

        float time = 0;
        while (time < popDuration)
        {
            float scale = Mathf.Lerp(popScale, 1f, time / popDuration);

            for (int j = 0; j < 4; j++)
            {
                Vector3 offset = vertices[vertexIndex + j] - center;
                vertices[vertexIndex + j] = center + offset * scale;
            }

            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            time += Time.deltaTime;
            yield return null;
        }
    }
}