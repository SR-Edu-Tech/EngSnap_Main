using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public static class U10_UI_Utils
    {
        private static Sprite cachedRoundedCardSprite;
        private static Sprite cachedRoundedButtonSprite;

        public static Sprite GetRoundedCardSprite()
        {
            if (cachedRoundedCardSprite != null) return cachedRoundedCardSprite;

            int w = 64, h = 64, r = 18;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Math.Max(0, Math.Max(r - x, x - (w - 1 - r)));
                    int dy = Math.Max(0, Math.Max(r - y, y - (h - 1 - r)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    colors[y * w + x] = dist <= r ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            cachedRoundedCardSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return cachedRoundedCardSprite;
        }

        public static Sprite GetRoundedButtonSprite()
        {
            if (cachedRoundedButtonSprite != null) return cachedRoundedButtonSprite;

            int w = 64, h = 64, r = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Math.Max(0, Math.Max(r - x, x - (w - 1 - r)));
                    int dy = Math.Max(0, Math.Max(r - y, y - (h - 1 - r)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    colors[y * w + x] = dist <= r ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            cachedRoundedButtonSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return cachedRoundedButtonSprite;
        }

        public static void ApplyRoundedCardStyle(Image img, Color color)
        {
            if (img == null) return;
            img.sprite = GetRoundedCardSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        public static void ApplyRoundedButtonStyle(Image img, Color color)
        {
            if (img == null) return;
            if (img.sprite == null)
            {
                img.sprite = GetRoundedButtonSprite();
                img.type = Image.Type.Sliced;
                img.color = color;
            }
        }

        public static void FormatHeaderTypography(Transform root, string titleText, string subtitleText = "Homonyms, Homophones & Homographs")
        {
            if (root == null) return;

            Transform tTr = root.Find("ProgressHUD/Title_Text") 
                         ?? root.Find("ProgressHUD/TitleText") 
                         ?? root.Find("HUD/Title_Text") 
                         ?? root.Find("Title_Text") 
                         ?? root.Find("Title");
            if (tTr != null)
            {
                var txt = tTr.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = $"<b>{titleText}</b>";
                    txt.fontSize = 48;
                    txt.fontStyle = FontStyles.Bold;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.color = Color.white;
                }
            }

            Transform sTr = root.Find("ProgressHUD/Subtitle_Text") 
                         ?? root.Find("ProgressHUD/SubtitleText") 
                         ?? root.Find("HUD/Subtitle_Text") 
                         ?? root.Find("Subtitle_Text") 
                         ?? root.Find("Subtitle");
            if (sTr != null)
            {
                var txt = sTr.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = $"<b>{subtitleText}</b>";
                    txt.fontSize = 32;
                    txt.fontStyle = FontStyles.Bold;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.color = new Color(0.75f, 0.9f, 1f);
                }
            }
        }
        public static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public static void EnsureHUD(Transform root, ref TextMeshProUGUI progressText, ref TextMeshProUGUI scoreText)
        {
            if (root == null) return;

            Transform hud = root.Find("ProgressHUD") ?? root.Find("HUD") ?? root;

            TMP_FontAsset fontAsset = null;
            var existingTmp = hud.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existingTmp != null) fontAsset = existingTmp.font;

            if (progressText == null)
            {
                Transform pTr = hud.Find("Progress_Text") 
                             ?? hud.Find("ProgressText") 
                             ?? root.Find("Progress_Text") 
                             ?? root.Find("ProgressText")
                             ?? FindDeepChild(root, "Progress_Text")
                             ?? FindDeepChild(root, "ProgressText");

                if (pTr != null)
                {
                    progressText = pTr.GetComponent<TextMeshProUGUI>();
                }
                else if (hud != null)
                {
                    GameObject pGo = new GameObject("Progress_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pGo.transform.SetParent(hud, false);
                    RectTransform pRt = pGo.GetComponent<RectTransform>();
                    pRt.anchoredPosition = new Vector2(-380, 15);
                    pRt.sizeDelta = new Vector2(260, 45);
                    progressText = pGo.GetComponent<TextMeshProUGUI>();
                    if (fontAsset != null) progressText.font = fontAsset;
                    progressText.fontSize = 28;
                    progressText.fontStyle = FontStyles.Bold;
                    progressText.alignment = TextAlignmentOptions.Left;
                    progressText.color = new Color(0.75f, 0.9f, 1f);
                    progressText.raycastTarget = false;
                }
            }

            if (scoreText == null)
            {
                Transform scTr = hud.Find("Score_Text") 
                              ?? hud.Find("ScoreText") 
                              ?? root.Find("Score_Text") 
                              ?? root.Find("ScoreText")
                              ?? FindDeepChild(root, "Score_Text")
                              ?? FindDeepChild(root, "ScoreText");

                if (scTr != null)
                {
                    scoreText = scTr.GetComponent<TextMeshProUGUI>();
                }
                else if (hud != null)
                {
                    GameObject scGo = new GameObject("Score_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    scGo.transform.SetParent(hud, false);
                    RectTransform scRt = scGo.GetComponent<RectTransform>();
                    scRt.anchoredPosition = new Vector2(380, 15);
                    scRt.sizeDelta = new Vector2(260, 45);
                    scoreText = scGo.GetComponent<TextMeshProUGUI>();
                    if (fontAsset != null) scoreText.font = fontAsset;
                    scoreText.fontSize = 28;
                    scoreText.fontStyle = FontStyles.Bold;
                    scoreText.alignment = TextAlignmentOptions.Right;
                    scoreText.color = new Color(1f, 0.85f, 0.25f);
                    scoreText.raycastTarget = false;
                }
            }
        }
    }
}
