using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EngSnap.Masters.Unit11 {

    /// <summary>
    /// Subclass for Unit 11: Art of Politeness - Speaking Lesson One (SP01: Say It Kindly).
    /// Inherits 100% of speech recognition, phrase card spawning, Levenshtein similarity, and UI flow
    /// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
    /// 6 speaking prompts: student listens to ARIA's blunt sentence and speaks the polite version.
    /// Note on screen: "The words matter, and so does the voice."
    /// </summary>
    public class Masters_ArtOfPoliteness_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

        public void SetSpeechToTextData(SpeechToText[] data) {
            speechToTextArray = data;
        }

        protected override void Awake() {
            topic = Masters_Topic.Speaking;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Speaking/artofpoliteness_sp01_full_intro.mp3");
#endif
            }

            EnsureReferencesBound();
            base.Awake();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Speaking;
            EnsureHeaderAndTitle();
        }

        private void EnsureHeaderAndTitle() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                if (tmp == null) continue;
                string n = tmp.gameObject.name.ToLower();
                Transform parent = tmp.transform.parent;
                string pn = parent != null ? parent.name.ToLower() : "";

                if (n.Contains("branch") || n.Contains("header") || pn.Contains("branch") || pn.Contains("header")) {
                    tmp.text = "THE ART OF POLITENESS";
                } else if (n.Contains("lessontitle") || pn.Contains("lessontitle") || (n == "tmp" && pn.Contains("title"))) {
                    tmp.text = "SP01 Say It Kindly";
                } else if (n.Contains("subtitle") || n.Contains("instruction") || pn.Contains("subtitle")) {
                    tmp.text = "The words matter, and so does the voice.";
                }

                if (tmp.text.Contains("🎩") || (tmp.text.Contains("THE ART OF POLITENESS") && tmp.text.Length > 22)) {
                    tmp.text = "THE ART OF POLITENESS";
                }
            }
        }

        private void EnsureReferencesBound() {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var baseType = typeof(Masters_PolishedCommunication_Speaking_LessonOne);

            // PhraseCardReference
            var cardRefField = baseType.GetField("phraseCardReferenceGameObject", flags);
            if (cardRefField != null && cardRefField.GetValue(this) == null) {
                Transform t = transform.Find("PhraseCardReference");
                if (t != null) cardRefField.SetValue(this, t.gameObject);
            }

            // PhraseCardSpawnPoint
            var spawnField = baseType.GetField("phraseCardSpawnPointRectTransform", flags);
            if (spawnField != null && spawnField.GetValue(this) == null) {
                Transform t = transform.Find("PhraseCardSpawnPoint");
                if (t != null) spawnField.SetValue(this, t.GetComponent<RectTransform>());
            }

            // DebugText
            var debugField = baseType.GetField("debugTMP", flags);
            if (debugField != null && debugField.GetValue(this) == null) {
                Transform t = transform.Find("DebugText") ?? transform.Find("DebugTMP");
                if (t != null) debugField.SetValue(this, t.GetComponent<TextMeshProUGUI>());
            }

            // ProgressCountTMP
            var progField = baseType.GetField("progressCountTMP", flags);
            if (progField != null && progField.GetValue(this) == null) {
                Transform t = transform.Find("ProgressCountTMP") ?? transform.Find("progression count");
                if (t != null) progField.SetValue(this, t.GetComponent<TextMeshProUGUI>());
            }

            // Skip Button
            var skipField = baseType.GetField("skipButton", flags);
            if (skipField != null && skipField.GetValue(this) == null) {
                Transform t = transform.Find("SkipButton") ?? transform.Find("Skip");
                if (t != null) skipField.SetValue(this, t.GetComponent<Button>());
            }

            // Continue Button
            var contField = baseType.GetField("continueButton", flags);
            if (contField != null && contField.GetValue(this) == null) {
                Transform t = transform.Find("Continue") ?? transform.Find("ContinueButton");
                if (t != null) contField.SetValue(this, t.GetComponent<Button>());
            }

            // Slider / ProgressBar
            var sliderField = baseType.GetField("progressBar", flags);
            if (sliderField != null && sliderField.GetValue(this) == null) {
                Slider s = GetComponentInChildren<Slider>(true);
                if (s != null) sliderField.SetValue(this, s);
            }
        }
    }
}
