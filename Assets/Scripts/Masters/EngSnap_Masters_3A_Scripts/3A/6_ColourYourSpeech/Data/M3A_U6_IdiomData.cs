using System;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.IdiomStudio
{
    public enum IdiomFamily
    {
        Easy,
        Crazy,
        Happy,
        Joking,
        Kind,
        Bossy
    }

    [Serializable]
    public struct IdiomEntry
    {
        public string idiomText;
        public string verbatimMeaning;
        public IdiomFamily family;
        public string[] sayInsteadSwaps;
        public string[] exampleSentences;
        public Sprite literalSprite;
        public Sprite realMeaningSprite;
        public AudioClip idiomVoiceClip;

        public IdiomEntry(
            string idiomText,
            string verbatimMeaning,
            IdiomFamily family,
            string[] sayInsteadSwaps = null,
            string[] exampleSentences = null,
            Sprite literalSprite = null,
            Sprite realMeaningSprite = null,
            AudioClip idiomVoiceClip = null)
        {
            this.idiomText = idiomText;
            this.verbatimMeaning = verbatimMeaning;
            this.family = family;
            this.sayInsteadSwaps = sayInsteadSwaps ?? Array.Empty<string>();
            this.exampleSentences = exampleSentences ?? Array.Empty<string>();
            this.literalSprite = literalSprite;
            this.realMeaningSprite = realMeaningSprite;
            this.idiomVoiceClip = idiomVoiceClip;
        }
    }

    public static class M3A_U6_IdiomData
    {
        public static readonly List<IdiomEntry> IdiomBank = new List<IdiomEntry>
        {
            new IdiomEntry(
                "My way or the highway",
                "You have to listen to me.",
                IdiomFamily.Bossy,
                new[] { "Do what I say or leave.", "You must follow my rule." },
                new[] { "It's my way or the highway when we work on this project." }
            ),
            new IdiomEntry(
                "Cloud nine",
                "In a state of happiness or bliss or extreme excitement.",
                IdiomFamily.Happy,
                new[] { "Very happy", "Extremely excited" },
                new[] { "He was on cloud nine when he heard the good news." }
            ),
            new IdiomEntry(
                "Tongue-in-cheek",
                "Saying something as a joke, not to be taken seriously.",
                IdiomFamily.Joking,
                new[] { "Just kidding", "As a joke" },
                new[] { "He made some tongue-in-cheek comment about his teammates." }
            ),
            new IdiomEntry(
                "A piece of cake",
                "Very easy.",
                IdiomFamily.Easy,
                new[] { "Simple", "Very easy" },
                new[] { "The exam paper was a piece of cake." }
            ),
            new IdiomEntry(
                "Easy as pie",
                "Very easy.",
                IdiomFamily.Easy,
                new[] { "Very simple", "Effortless" },
                new[] { "Solving this puzzle is as easy as pie." }
            ),
            new IdiomEntry(
                "Giving candy to a baby",
                "Very easy, especially when you do something wrong.",
                IdiomFamily.Easy,
                new[] { "Too easy to trick", "Effortless win" },
                new[] { "Winning against him was like giving candy to a baby." }
            ),
            new IdiomEntry(
                "Fruitcake",
                "Really strange or crazy.",
                IdiomFamily.Crazy,
                new[] { "Silly", "Really strange" },
                new[] { "You can be such a silly fruitcake sometimes." }
            ),
            new IdiomEntry(
                "Sugar and spice",
                "Behaving in a kind and friendly way; very sweet and nice.",
                IdiomFamily.Kind,
                new[] { "Very sweet and nice", "Kind and friendly" },
                new[] { "Moms are always full of sugar and spice." }
            ),
            new IdiomEntry(
                "Nut / Nutty",
                "Funny, kind of crazy, usually makes you laugh.",
                IdiomFamily.Crazy,
                new[] { "Funny and crazy", "Wildly amusing" },
                new[] { "His goofy dance made everyone call him a total nut." }
            ),
            new IdiomEntry(
                "Going bananas",
                "Becoming crazy, especially with too much to do / losing temper.",
                IdiomFamily.Crazy,
                new[] { "Losing temper", "Becoming crazy" },
                new[] { "If I'm late, my dad will go bananas." }
            )
        };

        public static IdiomEntry GetIdiomByText(string text)
        {
            return IdiomBank.Find(entry => entry.idiomText.Equals(text, StringComparison.OrdinalIgnoreCase));
        }

        public static List<IdiomEntry> GetIdiomsByFamily(IdiomFamily family)
        {
            return IdiomBank.FindAll(entry => entry.family == family);
        }
    }
}
