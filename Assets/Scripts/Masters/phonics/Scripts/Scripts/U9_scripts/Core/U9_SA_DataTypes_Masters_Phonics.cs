using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    // =========================================================================
    // Unit 9 Enums
    // =========================================================================

    public enum CleFamily
    {
        BLE,
        CLE,
        DLE,
        FLE,
        GLE,
        KLE,
        PLE,
        STLE,
        TLE,
        ZLE
    }

    public enum VowelLength
    {
        LongOpen,   // 1 consonant before -le -> Open syllable -> Long vowel (e.g., ta-ble, ma-ple)
        ShortClosed // 2 consonants before -le -> Closed syllable -> Short vowel (e.g., cat-tle, ap-ple)
    }

    public enum U9ChallengeQuestionType
    {
        Text_MCQ,               // Multiple choice concept questions
        Splitter,               // Place syllable divider at turtle rule gap
        OneOrTwo_Swipe,         // Decide long vs short first vowel
        Build_Ending,           // Join base + ending with doubling decision
        Sentence_Hunt,          // Tap the c+le word inside a sentence
        Transfer_Unseen         // Deduce pronunciation of unseen word 'ogle'
    }

    // =========================================================================
    // Activity Data Models
    // =========================================================================

    [Serializable]
    public class TurtleSplitItem
    {
        public string fullWord;
        public string splitWord; // e.g., "tur|tle"
        public string[] chunks;  // ["tur", "tle"]
        public int correctGapIndex; // Gap index where the split line belongs
        public CleFamily family;
        public bool isCkException; // e.g. pick|le, buck|le
        public bool isBonus;       // Multi-split words: bi|cy|cle, ve|hi|cle

        public TurtleSplitItem(string word, string split, CleFamily fam, bool ckException = false, bool bonus = false)
        {
            fullWord = word;
            splitWord = split;
            family = fam;
            isCkException = ckException;
            isBonus = bonus;
            chunks = split.Split('|');
            // Gap index is length of first chunk
            correctGapIndex = chunks.Length > 0 ? chunks[0].Length : word.Length - 3;
        }
    }

    [Serializable]
    public class OneOrTwoItem
    {
        public string fullWord;
        public string splitWord;
        public VowelLength vowelLength;
        public int consonantsBeforeLe; // 1 or 2
        public string firstSyllable;
        public string endingChunk;

        public OneOrTwoItem(string word, string split, VowelLength length, int consonants)
        {
            fullWord = word;
            splitWord = split;
            vowelLength = length;
            consonantsBeforeLe = consonants;
            string[] parts = split.Split('|');
            firstSyllable = parts.Length > 0 ? parts[0] : "";
            endingChunk = parts.Length > 1 ? parts[1] : "";
        }
    }

    [Serializable]
    public class BuildEndingItem
    {
        public string baseChunk;
        public string endingChunk; // "dle", "gle", "tle", "ble", "cle", "fle", "ple", "zle"
        public bool shouldDoubleLastLetter;
        public string finalWord;
        public bool isRoundBOpen; // Round A = closed bases, Round B = open vowel bases

        public BuildEndingItem(string basePart, string ending, bool doubleLast, string word, bool roundB = false)
        {
            baseChunk = basePart;
            endingChunk = ending;
            shouldDoubleLastLetter = doubleLast;
            finalWord = word;
            isRoundBOpen = roundB;
        }
    }

    [Serializable]
    public class SentenceHuntItem
    {
        public string sentenceText;
        public string[] targetCleWords; // Words in sentence that end in C+le
        public string[] splitRepresentations; // e.g., "cas|tle"
        public bool isBonus; // Sentences with 2+ Cle words

        public SentenceHuntItem(string sentence, string[] words, string[] splits, bool bonus = false)
        {
            sentenceText = sentence;
            targetCleWords = words;
            splitRepresentations = splits;
            isBonus = bonus;
        }
    }

    [Serializable]
    public class U9ChallengeQuestion
    {
        public int questionId;
        public int questionNumber => questionId;
        public U9ChallengeQuestionType questionType;
        public string promptText;
        public string[] options;
        public int correctOptionIndex;
        public string targetWord;
        public string splitWord;
        public string explanation;

        public U9ChallengeQuestion(int id, U9ChallengeQuestionType type, string prompt, string[] opt, int correctIdx, string word = "", string split = "", string exp = "")
        {
            questionId = id;
            questionType = type;
            promptText = prompt;
            options = opt;
            correctOptionIndex = correctIdx;
            targetWord = word;
            splitWord = split;
            explanation = exp;
        }
    }

    // =========================================================================
    // Static Fallback Data Providers
    // =========================================================================

    public static class U9_DataBank
    {
        // ---------------------------------------------------------------------
        // Activity 1: The Turtle Rule (16 words + 4 bonus)
        // ---------------------------------------------------------------------
        public static List<TurtleSplitItem> GetActivity1SplitPool()
        {
            return new List<TurtleSplitItem>
            {
                new TurtleSplitItem("humble", "hum|ble", CleFamily.BLE),
                new TurtleSplitItem("table", "ta|ble", CleFamily.BLE),
                new TurtleSplitItem("cycle", "cy|cle", CleFamily.CLE),
                new TurtleSplitItem("circle", "cir|cle", CleFamily.CLE),
                new TurtleSplitItem("waffle", "waf|fle", CleFamily.FLE),
                new TurtleSplitItem("sniffle", "snif|fle", CleFamily.FLE),
                new TurtleSplitItem("ankle", "an|kle", CleFamily.KLE),
                new TurtleSplitItem("sprinkle", "sprin|kle", CleFamily.KLE),
                new TurtleSplitItem("buckle", "buck|le", CleFamily.KLE, true), // ck exception
                new TurtleSplitItem("candle", "can|dle", CleFamily.DLE),
                new TurtleSplitItem("paddle", "pad|dle", CleFamily.DLE),
                new TurtleSplitItem("eagle", "ea|gle", CleFamily.GLE),
                new TurtleSplitItem("bugle", "bu|gle", CleFamily.GLE),
                new TurtleSplitItem("apple", "ap|ple", CleFamily.PLE),
                new TurtleSplitItem("purple", "pur|ple", CleFamily.PLE),
                new TurtleSplitItem("turtle", "tur|tle", CleFamily.TLE),
                new TurtleSplitItem("cattle", "cat|tle", CleFamily.TLE),
                new TurtleSplitItem("puzzle", "puz|zle", CleFamily.ZLE),
                new TurtleSplitItem("sizzle", "siz|zle", CleFamily.ZLE),
                new TurtleSplitItem("castle", "cas|tle", CleFamily.STLE)
            };
        }

        public static List<TurtleSplitItem> GetActivity1BonusSplitPool()
        {
            return new List<TurtleSplitItem>
            {
                new TurtleSplitItem("bicycle", "bi|cy|cle", CleFamily.CLE, false, true),
                new TurtleSplitItem("vehicle", "ve|hi|cle", CleFamily.CLE, false, true),
                new TurtleSplitItem("triangle", "tri|an|gle", CleFamily.GLE, false, true),
                new TurtleSplitItem("uncle", "un|cle", CleFamily.CLE, false, true)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 2: One or Two? (16 balanced words: 8 Long/Open vs 8 Short/Closed)
        // ---------------------------------------------------------------------
        public static List<OneOrTwoItem> GetActivity2Pool()
        {
            return new List<OneOrTwoItem>
            {
                // One Consonant -> Long Vowel (Open)
                new OneOrTwoItem("table", "ta|ble", VowelLength.LongOpen, 1),
                new OneOrTwoItem("maple", "ma|ple", VowelLength.LongOpen, 1),
                new OneOrTwoItem("noble", "no|ble", VowelLength.LongOpen, 1),
                new OneOrTwoItem("bugle", "bu|gle", VowelLength.LongOpen, 1),
                new OneOrTwoItem("cycle", "cy|cle", VowelLength.LongOpen, 1),
                new OneOrTwoItem("idle", "i|dle", VowelLength.LongOpen, 1),
                new OneOrTwoItem("eagle", "ea|gle", VowelLength.LongOpen, 1),
                new OneOrTwoItem("staple", "sta|ple", VowelLength.LongOpen, 1),

                // Two Consonants -> Short Vowel (Closed)
                new OneOrTwoItem("cattle", "cat|tle", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("apple", "ap|ple", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("bottle", "bot|tle", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("puzzle", "puz|zle", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("little", "lit|tle", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("paddle", "pad|dle", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("candle", "can|dle", VowelLength.ShortClosed, 2),
                new OneOrTwoItem("muzzle", "muz|zle", VowelLength.ShortClosed, 2)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 3: Build the Ending (12 items: 6 Round A + 6 Round B)
        // ---------------------------------------------------------------------
        public static List<BuildEndingItem> GetActivity3Pool()
        {
            return new List<BuildEndingItem>
            {
                // Round A: Closed bases (short vowels, mostly doubling)
                new BuildEndingItem("can", "dle", false, "candle", false),
                new BuildEndingItem("sad", "dle", true, "saddle", false),
                new BuildEndingItem("jug", "gle", true, "juggle", false),
                new BuildEndingItem("wig", "gle", true, "wiggle", false),
                new BuildEndingItem("mid", "dle", true, "middle", false),
                new BuildEndingItem("bot", "tle", true, "bottle", false),

                // Round B: Open bases (long vowels, no doubling)
                new BuildEndingItem("a", "ble", false, "able", true),
                new BuildEndingItem("fa", "ble", false, "fable", true),
                new BuildEndingItem("ca", "ble", false, "cable", true),
                new BuildEndingItem("ma", "ple", false, "maple", true),
                new BuildEndingItem("sta", "ple", false, "staple", true),
                new BuildEndingItem("no", "ble", false, "noble", true)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 4: Sentence Hunt (10 standard + 3 bonus sentences)
        // ---------------------------------------------------------------------
        public static List<SentenceHuntItem> GetActivity4Pool()
        {
            return new List<SentenceHuntItem>
            {
                new SentenceHuntItem("I play the bugle.", new[] { "bugle" }, new[] { "bu|gle" }),
                new SentenceHuntItem("Eva likes to play with the bubbles.", new[] { "bubbles" }, new[] { "bub|bles" }),
                new SentenceHuntItem("My favourite colour is purple.", new[] { "purple" }, new[] { "pur|ple" }),
                new SentenceHuntItem("The maple tree is very large.", new[] { "maple" }, new[] { "ma|ple" }),
                new SentenceHuntItem("The boy giggled at the joke.", new[] { "giggled" }, new[] { "gig|gled" }),
                new SentenceHuntItem("We have a pet turtle at home.", new[] { "turtle" }, new[] { "tur|tle" }),
                new SentenceHuntItem("I saw the castle on the hill.", new[] { "castle" }, new[] { "cas|tle" }),
                new SentenceHuntItem("I will eat the waffle for breakfast.", new[] { "waffle" }, new[] { "waf|fle" }),
                new SentenceHuntItem("She likes to cuddle the puppy.", new[] { "cuddle" }, new[] { "cud|dle" }),
                new SentenceHuntItem("We will eat dinner on the table.", new[] { "table" }, new[] { "ta|ble" })
            };
        }

        public static List<SentenceHuntItem> GetActivity4BonusPool()
        {
            return new List<SentenceHuntItem>
            {
                new SentenceHuntItem("The little turtle stumbles into the puddle.", new[] { "little", "turtle", "stumbles", "puddle" }, new[] { "lit|tle", "tur|tle", "stum|bles", "pud|dle" }, true),
                new SentenceHuntItem("I can juggle an apple and a bottle.", new[] { "juggle", "apple", "bottle" }, new[] { "jug|gle", "ap|ple", "bot|tle" }, true),
                new SentenceHuntItem("The eagle circled above the castle.", new[] { "eagle", "circled", "castle" }, new[] { "ea|gle", "cir|cled", "cas|tle" }, true)
            };
        }

        // ---------------------------------------------------------------------
        // Unit Challenge: 10 Mixed Questions
        // ---------------------------------------------------------------------
        public static List<U9ChallengeQuestion> GetChallengeQuestions()
        {
            return new List<U9ChallengeQuestion>
            {
                new U9ChallengeQuestion(1, U9ChallengeQuestionType.Text_MCQ, "In a consonant + le syllable, the 'e' is...", new[] { "silent", "long", "short" }, 0, "", "", "The 'e' is silent in C+le syllables."),
                new U9ChallengeQuestion(2, U9ChallengeQuestionType.Splitter, "Divide 'sprinkle' using the Turtle Rule:", new[] { "sprin|kle", "sp|rinkle", "sprink|le" }, 0, "sprinkle", "sprin|kle", "Count back three: e... l... k -> sprin | kle."),
                new U9ChallengeQuestion(3, U9ChallengeQuestionType.Text_MCQ, "The turtle rule says: count back...", new[] { "three letters", "two letters", "four letters" }, 0, "", "", "Always count back three letters from the end of the word."),
                new U9ChallengeQuestion(4, U9ChallengeQuestionType.OneOrTwo_Swipe, "Does 'bugle' have a long or short first vowel?", new[] { "Long Vowel (Open)", "Short Vowel (Closed)" }, 0, "bugle", "bu|gle", "One consonant before -le means the first syllable is open (long /juː/)."),
                new U9ChallengeQuestion(5, U9ChallengeQuestionType.OneOrTwo_Swipe, "Does 'bottle' have a long or short first vowel?", new[] { "Long Vowel (Open)", "Short Vowel (Closed)" }, 1, "bottle", "bot|tle", "Two consonants before -le means the first syllable is closed (short /ɒ/)."),
                new U9ChallengeQuestion(6, U9ChallengeQuestionType.Build_Ending, "Build base 'wig' + ending 'gle'. Do you double 'g'?", new[] { "wiggle (Double)", "wigle (No double)" }, 0, "wiggle", "wig|gle", "Closed base with short vowel doubles the consonant: wiggle."),
                new U9ChallengeQuestion(7, U9ChallengeQuestionType.Sentence_Hunt, "Find the C+le words in: 'The gentle eagle landed.'", new[] { "gentle, eagle", "landed", "gentle only" }, 0, "gentle eagle", "gen|tle, ea|gle", "Both 'gentle' and 'eagle' end in consonant + le."),
                new U9ChallengeQuestion(8, U9ChallengeQuestionType.Text_MCQ, "Why does the -le syllable need an 'e'?", new[] { "Every syllable needs a vowel letter", "To make the vowel long", "To make it plural" }, 0, "", "", "Every English syllable must contain a written vowel letter."),
                new U9ChallengeQuestion(9, U9ChallengeQuestionType.Splitter, "Divide 'candle' using the Turtle Rule:", new[] { "can|dle", "cand|le", "ca|ndle" }, 0, "candle", "can|dle", "Count back three: e... l... d -> can | dle."),
                new U9ChallengeQuestion(10, U9ChallengeQuestionType.Transfer_Unseen, "You've never seen the word 'ogle'. Is the 'o' long or short?", new[] { "Long Vowel (OH-gul)", "Short Vowel (OG-ul)" }, 0, "ogle", "o|gle", "One consonant 'g' before -le means the first syllable is open -> Long 'o' (OH-gul).")
            };
        }
    }

    // =========================================================================
    // UI Styling & Rounded Edge Utilities
    // =========================================================================

    public static class U9_UI_Utils
    {
        private static Sprite cachedRoundedSprite;

        public static Sprite GetOrCreateRoundedSprite(int size = 128, int radius = 28)
        {
            if (cachedRoundedSprite != null) return cachedRoundedSprite;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            cachedRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return cachedRoundedSprite;
        }

        public static void ApplyRoundedCardStyle(UnityEngine.UI.Image image, Color color)
        {
            if (image == null) return;
            if (image.sprite == null)
            {
                image.sprite = GetOrCreateRoundedSprite(128, 28);
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = color;
            }
        }

        public static void ApplyRoundedButtonStyle(UnityEngine.UI.Image image, Color color)
        {
            if (image == null) return;
            if (image.sprite == null)
            {
                image.sprite = GetOrCreateRoundedSprite(128, 24);
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = color;
            }
        }

        public static void FormatHeaderTypography(Transform root, string defaultTitle, string defaultSubtitle = "Consonant + le Syllables")
        {
            if (root == null) return;
            Transform hud = root.Find("ProgressHUD") ?? root.Find("HUD") ?? root.Find("Header_Container") ?? root.Find("HeaderRibbon") ?? root.Find("Header") ?? root;

            // Title (Bold, Size 45)
            Transform tTr = hud.Find("Title_Text") ?? hud.Find("TitleText") ?? hud.Find("Title") ?? root.Find("Title_Text") ?? root.Find("Title");
            if (tTr != null)
            {
                var txt = tTr.GetComponent<TMPro.TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.fontSize = 45;
                    txt.fontStyle = TMPro.FontStyles.Bold;
                    txt.alignment = TMPro.TextAlignmentOptions.Center;
                    if (string.IsNullOrEmpty(txt.text) || txt.text == "Title") txt.text = $"<b>{defaultTitle}</b>";
                    else if (!txt.text.Contains("<b>")) txt.text = $"<b>{txt.text}</b>";
                    RectTransform rt = tTr.GetComponent<RectTransform>();
                    if (rt != null && rt.sizeDelta.y < 55) rt.sizeDelta = new Vector2(rt.sizeDelta.x > 0 ? rt.sizeDelta.x : 950, 60);
                }
            }

            // Subtitle (Bold, Size 30)
            Transform sTr = hud.Find("Subtitle_Text") ?? hud.Find("SubtitleText") ?? hud.Find("Subtitle") ?? hud.Find("Prompt_Text") ?? root.Find("Subtitle_Text") ?? root.Find("Subtitle");
            if (sTr != null)
            {
                var txt = sTr.GetComponent<TMPro.TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.fontSize = 30;
                    txt.fontStyle = TMPro.FontStyles.Bold;
                    txt.alignment = TMPro.TextAlignmentOptions.Center;
                    if (string.IsNullOrEmpty(txt.text) || txt.text == "Subtitle") txt.text = $"<b>{defaultSubtitle}</b>";
                    else if (!txt.text.Contains("<b>")) txt.text = $"<b>{txt.text}</b>";
                    RectTransform rt = sTr.GetComponent<RectTransform>();
                    if (rt != null && rt.sizeDelta.y < 40) rt.sizeDelta = new Vector2(rt.sizeDelta.x > 0 ? rt.sizeDelta.x : 950, 45);
                }
            }
        }
    }
}
