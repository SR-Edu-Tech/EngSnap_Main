using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public enum U10CategoryType
    {
        Homophone,   // Same sound, different spelling & meaning (see / sea)
        Homonym,     // Same sound & spelling, different meaning (bat / bat)
        Homograph    // Same spelling, different sound & meaning (read / read)
    }

    public enum U10ChallengeQuestionType
    {
        Text_MCQ,
        Which_One_Fits,
        Sound_Twins,
        Two_Meanings,
        Say_It_Two_Ways,
        Transfer_Stress
    }

    // =========================================================================
    // Data Structs for Concept Cards, Activities & Challenge
    // =========================================================================

    [Serializable]
    public class U10ConceptCard
    {
        public int cardIndex;
        public string heading;
        public string subHeading;
        public string bodyText;
        public string[] exampleTerms;
        public string audioKey;

        public U10ConceptCard(int idx, string head, string sub, string body, string[] examples, string audio)
        {
            cardIndex = idx;
            heading = head;
            subHeading = sub;
            bodyText = body;
            exampleTerms = examples;
            audioKey = audio;
        }
    }

    [Serializable]
    public class U10MatchCardItem
    {
        public string word;
        public int matchGroupId;
        public int groupSize; // 2 for pair, 3 for 3-way set

        public U10MatchCardItem(string w, int groupId, int size = 2)
        {
            word = w;
            matchGroupId = groupId;
            groupSize = size;
        }
    }

    [Serializable]
    public class U10WhichOneFitsItem
    {
        public int itemId;
        public string sentenceTemplate; // e.g., "I ______ fruit at the supermarket."
        public string[] options;        // e.g., ["way", "weigh"]
        public int correctIndex;
        public string fullCorrectSentence; // e.g., "I weigh fruit at the supermarket."
        public bool isHighFrequencyRoundB;

        public U10WhichOneFitsItem(int id, string template, string[] opt, int correctIdx, string fullSentence, bool isHf = false)
        {
            itemId = id;
            sentenceTemplate = template;
            options = opt;
            correctIndex = correctIdx;
            fullCorrectSentence = fullSentence;
            isHighFrequencyRoundB = isHf;
        }
    }

    [Serializable]
    public class U10TwoMeaningsItem
    {
        public int itemId;
        public string sentenceText; // "The bat flew out of the cave."
        public string homonymWord;  // "bat"
        public string meaningA;     // "an animal"
        public string meaningB;     // "sports equipment"
        public int correctMeaningIndex; // 0 for A, 1 for B
        public string illustrationKey;

        public U10TwoMeaningsItem(int id, string sent, string word, string mA, string mB, int correctIdx, string illus = "")
        {
            itemId = id;
            sentenceText = sent;
            homonymWord = word;
            meaningA = mA;
            meaningB = mB;
            correctMeaningIndex = correctIdx;
            illustrationKey = illus;
        }
    }

    [Serializable]
    public class U10SayItTwoWaysItem
    {
        public int itemId;
        public string sentenceText;        // "I read that book last year."
        public string homographWord;       // "read"
        public string pronunciationLabelA; // "\"red\" (past tense)"
        public string pronunciationLabelB; // "\"reed\" (present tense)"
        public string audioKeyA;           // "read_past"
        public string audioKeyB;           // "read_pres"
        public int correctOptionIndex;     // 0 for A, 1 for B
        public string grammarRuleNote;     // "Unit 4 Stress / Vowel Shift"

        public U10SayItTwoWaysItem(int id, string sent, string word, string labelA, string labelB, string aKeyA, string aKeyB, int correctIdx, string note = "")
        {
            itemId = id;
            sentenceText = sent;
            homographWord = word;
            pronunciationLabelA = labelA;
            pronunciationLabelB = labelB;
            audioKeyA = aKeyA;
            audioKeyB = aKeyB;
            correctOptionIndex = correctIdx;
            grammarRuleNote = note;
        }
    }

    [Serializable]
    public class U10ChallengeQuestion
    {
        public int questionId;
        public int questionNumber => questionId;
        public U10ChallengeQuestionType questionType;
        public string promptText;
        public string[] options;
        public int correctOptionIndex;
        public string targetWord;
        public string audioKey;
        public string explanation;

        public U10ChallengeQuestion(int id, U10ChallengeQuestionType type, string prompt, string[] opt, int correctIdx, string word = "", string audio = "", string exp = "")
        {
            questionId = id;
            questionType = type;
            promptText = prompt;
            options = opt;
            correctOptionIndex = correctIdx;
            targetWord = word;
            audioKey = audio;
            explanation = exp;
        }
    }

    // =========================================================================
    // Static Fallback DataBank
    // =========================================================================

    public static class U10_DataBank
    {
        // ---------------------------------------------------------------------
        // 1. Concept Cards (4 Cards)
        // ---------------------------------------------------------------------
        public static List<U10ConceptCard> GetConceptCards()
        {
            return new List<U10ConceptCard>
            {
                new U10ConceptCard(
                    0,
                    "Break the Word Apart",
                    "The words tell you what they mean!",
                    "<b>homo-</b> is a prefix meaning <b>same</b>.\n\n" +
                    "• <b>homo + nym</b> = same + name (same word/name)\n" +
                    "• <b>homo + phone</b> = same + sound (sounds identical)\n" +
                    "• <b>homo + graph</b> = same + writing (spelled identical)\n\n" +
                    "<i>Bonus: -phone is in telephone; -graph is in photograph!</i>",
                    new[] { "homonym", "homophone", "homograph", "telephone", "photograph" },
                    "U10_VO_card1"
                ),
                new U10ConceptCard(
                    1,
                    "Homophones: Same Sound",
                    "Same sound, different spelling & meaning",
                    "Two or more words that <b>sound identical</b> but have <b>different spellings</b> and different meanings.\n\n" +
                    "• <b>see</b> (look with eyes) vs <b>sea</b> (ocean waves)\n" +
                    "• <b>flour</b> (for baking) vs <b>flower</b> (in a garden)\n" +
                    "• <b>hear</b> (with ears) vs <b>here</b> (this place)\n\n" +
                    "<i>Listen: they sound exactly the same! Only meaning decides the spelling.</i>",
                    new[] { "see / sea", "flour / flower", "hear / here", "their / there", "to / too / two" },
                    "U10_VO_card2"
                ),
                new U10ConceptCard(
                    2,
                    "Homonyms: Same Word, Two Jobs",
                    "One spelling, one sound, multiple meanings",
                    "<b>One exact word</b> that keeps the <b>same spelling and sound</b>, but does different jobs depending on the sentence.\n\n" +
                    "• <i>The <b>bat</b> flew out of the cave.</i> (animal)\n" +
                    "• <i>He hit the ball with a <b>bat</b>.</i> (cricket bat)\n\n" +
                    "<i>The word never changes! Only the sentence tells you what it means.</i>",
                    new[] { "bat (animal / equipment)", "bark (dog / tree)", "fit (tantrum / match)", "bear (animal / endure)" },
                    "U10_VO_card3"
                ),
                new U10ConceptCard(
                    3,
                    "Homographs: Say It Two Ways",
                    "Same spelling, different sound & meaning",
                    "Words that have the <b>same spelling</b>, but are <b>pronounced differently</b> depending on the sentence!\n\n" +
                    "• <i>I <b>read</b> that book yesterday.</i> (said \"red\" — past tense)\n" +
                    "• <i>I <b>read</b> a book every night.</i> (said \"reed\" — present tense)\n" +
                    "• <i>The pipe is made of <b>lead</b>.</i> (said \"led\") vs <i>You <b>lead</b> the way.</i> (said \"leed\")\n\n" +
                    "<i>Only the sentence gives you the clue to pronounce it right!</i>",
                    new[] { "read (red / reed)", "lead (led / leed)", "wind (wynd / winned)", "live (liv / lyve)", "record (RE-cord / re-CORD)" },
                    "U10_VO_card4"
                )
            };
        }

        // ---------------------------------------------------------------------
        // 2. Activity 1: Sound Twins (Match Pairs)
        // ---------------------------------------------------------------------
        public static List<U10MatchCardItem> GetSoundTwinsRoundA()
        {
            // 16 Cards (8 Pairs from Workbook Page 50)
            return new List<U10MatchCardItem>
            {
                new U10MatchCardItem("sea", 1),
                new U10MatchCardItem("see", 1),
                new U10MatchCardItem("flower", 2),
                new U10MatchCardItem("flour", 2),
                new U10MatchCardItem("four", 3),
                new U10MatchCardItem("for", 3),
                new U10MatchCardItem("hear", 4),
                new U10MatchCardItem("here", 4),
                new U10MatchCardItem("male", 5),
                new U10MatchCardItem("mail", 5),
                new U10MatchCardItem("bee", 6),
                new U10MatchCardItem("be", 6),
                new U10MatchCardItem("tail", 7),
                new U10MatchCardItem("tale", 7),
                new U10MatchCardItem("knight", 8),
                new U10MatchCardItem("night", 8)
            };
        }

        public static List<U10MatchCardItem> GetSoundTwinsRoundB()
        {
            // High-Frequency Sets including 3-way sets
            return new List<U10MatchCardItem>
            {
                new U10MatchCardItem("their", 1, 3),
                new U10MatchCardItem("there", 1, 3),
                new U10MatchCardItem("they're", 1, 3),
                new U10MatchCardItem("to", 2, 3),
                new U10MatchCardItem("too", 2, 3),
                new U10MatchCardItem("two", 2, 3),
                new U10MatchCardItem("your", 3, 2),
                new U10MatchCardItem("you're", 3, 2),
                new U10MatchCardItem("sun", 4, 2),
                new U10MatchCardItem("son", 4, 2),
                new U10MatchCardItem("write", 5, 2),
                new U10MatchCardItem("right", 5, 2),
                new U10MatchCardItem("new", 6, 2),
                new U10MatchCardItem("knew", 6, 2),
                new U10MatchCardItem("would", 7, 2),
                new U10MatchCardItem("wood", 7, 2)
            };
        }

        // ---------------------------------------------------------------------
        // 3. Activity 2: Which One Fits? (16 Sentence Items)
        // ---------------------------------------------------------------------
        public static List<U10WhichOneFitsItem> GetWhichOneFitsItems()
        {
            return new List<U10WhichOneFitsItem>
            {
                // Round A: Page 50 Sentences (10 items)
                new U10WhichOneFitsItem(1, "Which ______ should I go?", new[] { "way", "weigh" }, 0, "Which way should I go?"),
                new U10WhichOneFitsItem(2, "I ______ fruit at the supermarket.", new[] { "way", "weigh" }, 1, "I weigh fruit at the supermarket."),
                new U10WhichOneFitsItem(3, "I dye my ______.", new[] { "hair", "hare" }, 0, "I dye my hair."),
                new U10WhichOneFitsItem(4, "A ______ looks a little like a rabbit.", new[] { "hair", "hare" }, 1, "A hare looks a little like a rabbit."),
                new U10WhichOneFitsItem(5, "A heroic act is called a ______.", new[] { "feat", "feet" }, 0, "A heroic act is called a feat."),
                new U10WhichOneFitsItem(6, "You walk on your ______.", new[] { "feat", "feet" }, 1, "You walk on your feet."),
                new U10WhichOneFitsItem(7, "The ______ rode his horse.", new[] { "knight", "night" }, 0, "The knight rode his horse."),
                new U10WhichOneFitsItem(8, "The moon comes out at ______.", new[] { "knight", "night" }, 1, "The moon comes out at night."),
                new U10WhichOneFitsItem(9, "Use the ______ to go up and down.", new[] { "stairs", "stares" }, 0, "Use the stairs to go up and down."),
                new U10WhichOneFitsItem(10, "The cat ______ at the mouse.", new[] { "stairs", "stares" }, 1, "The cat stares at the mouse."),

                // Round B: High-Frequency Confusion Sets (6 items, weighted 2x)
                new U10WhichOneFitsItem(11, "______ books are on the table.", new[] { "Their", "There", "They're" }, 0, "Their books are on the table.", true),
                new U10WhichOneFitsItem(12, "Put it over ______.", new[] { "their", "there", "they're" }, 1, "Put it over there.", true),
                new U10WhichOneFitsItem(13, "I have ______ brothers.", new[] { "to", "too", "two" }, 2, "I have two brothers.", true),
                new U10WhichOneFitsItem(14, "This bag is ______ heavy.", new[] { "to", "too", "two" }, 1, "This bag is too heavy.", true),
                new U10WhichOneFitsItem(15, "Is this ______ pencil?", new[] { "your", "you're" }, 0, "Is this your pencil?", true),
                new U10WhichOneFitsItem(16, "I ______ the answer already.", new[] { "new", "knew" }, 1, "I knew the answer already.", true)
            };
        }

        // ---------------------------------------------------------------------
        // 4. Activity 3: Two Meanings (14 Paired Homonym Items)
        // ---------------------------------------------------------------------
        public static List<U10TwoMeaningsItem> GetTwoMeaningsItems()
        {
            return new List<U10TwoMeaningsItem>
            {
                // Round A: The Book's Poem (8 paired items)
                new U10TwoMeaningsItem(1, "I threw such a fit when I lost.", "fit", "a tantrum", "it matches", 0, "fit_tantrum"),
                new U10TwoMeaningsItem(2, "These shoes don't fit me.", "fit", "a tantrum", "it matches", 1, "fit_shoes"),
                new U10TwoMeaningsItem(3, "The lions might be hungry.", "might", "perhaps", "strength & power", 0, "might_perhaps"),
                new U10TwoMeaningsItem(4, "The lion showed its might.", "might", "perhaps", "strength & power", 1, "might_power"),
                new U10TwoMeaningsItem(5, "The monkeys swing from the vine.", "swing", "to sway back and forth", "a hanging seat", 0, "swing_monkeys"),
                new U10TwoMeaningsItem(6, "She sat on the swing in the park.", "swing", "to sway back and forth", "a hanging seat", 1, "swing_park"),
                new U10TwoMeaningsItem(7, "The bear roared loudly.", "bear", "a large animal", "to carry or endure", 0, "bear_animal"),
                new U10TwoMeaningsItem(8, "I can't bear that noise.", "bear", "a large animal", "to carry or endure", 1, "bear_endure"),

                // Round B: Everyday Homonyms (6 paired items)
                new U10TwoMeaningsItem(9, "The bat flew out of the cave.", "bat", "a flying animal", "cricket/sports equipment", 0, "bat_animal"),
                new U10TwoMeaningsItem(10, "He hit the ball with the bat.", "bat", "a flying animal", "cricket/sports equipment", 1, "bat_cricket"),
                new U10TwoMeaningsItem(11, "The dog began to bark.", "bark", "a dog's sound", "a tree's outer skin", 0, "bark_dog"),
                new U10TwoMeaningsItem(12, "The bark on that tree is rough.", "bark", "a dog's sound", "a tree's outer skin", 1, "bark_tree"),
                new U10TwoMeaningsItem(13, "Turn on the light.", "light", "illumination (not dark)", "lightweight (not heavy)", 0, "light_lamp"),
                new U10TwoMeaningsItem(14, "This box is very light.", "light", "illumination (not dark)", "lightweight (not heavy)", 1, "light_box")
            };
        }

        // ---------------------------------------------------------------------
        // 5. Activity 4: Say It Two Ways (14 Genuine Homograph Items)
        // ---------------------------------------------------------------------
        public static List<U10SayItTwoWaysItem> GetSayItTwoWaysItems()
        {
            return new List<U10SayItTwoWaysItem>
            {
                // Round A: Vowel Shift Homographs (8 items)
                new U10SayItTwoWaysItem(1, "I read that book last year.", "read", "say \"red\" (past)", "say \"reed\" (present)", "read_past", "read_pres", 0, "Past tense vowel shift"),
                new U10SayItTwoWaysItem(2, "I read every night before bed.", "read", "say \"red\" (past)", "say \"reed\" (present)", "read_past", "read_pres", 1, "Present tense vowel"),
                new U10SayItTwoWaysItem(3, "The pipe is made of lead.", "lead", "say \"led\" (heavy metal)", "say \"leed\" (to guide)", "lead_metal", "lead_guide", 0, "Heavy metal noun"),
                new U10SayItTwoWaysItem(4, "You lead, and I'll follow.", "lead", "say \"led\" (heavy metal)", "say \"leed\" (to guide)", "lead_metal", "lead_guide", 1, "Action verb to guide"),
                new U10SayItTwoWaysItem(5, "Please wind the clock.", "wind", "say \"wynd\" (turn/twist)", "say \"winned\" (moving air)", "wind_turn", "wind_air", 0, "Verb to turn"),
                new U10SayItTwoWaysItem(6, "The wind blew the door shut.", "wind", "say \"wynd\" (turn/twist)", "say \"winned\" (moving air)", "wind_turn", "wind_air", 1, "Noun moving breeze"),
                new U10SayItTwoWaysItem(7, "Fish live in water.", "live", "say \"liv\" (verb: reside)", "say \"lyve\" (adj: in person)", "live_verb", "live_adj", 0, "Verb to be alive"),
                new U10SayItTwoWaysItem(8, "We watched a live match.", "live", "say \"liv\" (verb: reside)", "say \"lyve\" (adj: in person)", "live_verb", "live_adj", 1, "Adjective live show"),

                // Round B: Consonant & Stress Shift Homographs (6 items)
                new U10SayItTwoWaysItem(9, "Please close the door.", "close", "say \"cloze\" (to shut)", "say \"closs\" (nearby)", "close_shut", "close_near", 0, "Verb with /z/ sound"),
                new U10SayItTwoWaysItem(10, "My house is close to school.", "close", "say \"cloze\" (to shut)", "say \"closs\" (nearby)", "close_shut", "close_near", 1, "Adjective with /s/ sound"),
                new U10SayItTwoWaysItem(11, "Don't tear the paper.", "tear", "say \"tair\" (to rip)", "say \"teer\" (from eye)", "tear_rip", "tear_eye", 0, "Verb to rip apart"),
                new U10SayItTwoWaysItem(12, "A tear rolled down her cheek.", "tear", "say \"tair\" (to rip)", "say \"teer\" (from eye)", "tear_rip", "tear_eye", 1, "Noun teardrop"),
                new U10SayItTwoWaysItem(13, "She broke the world record.", "record", "RE-cord (Noun: stress 1st)", "re-CORD (Verb: stress 2nd)", "record_noun", "record_verb", 0, "Unit 4 Stress: Noun on 1st beat"),
                new U10SayItTwoWaysItem(14, "Press this to record the song.", "record", "RE-cord (Noun: stress 1st)", "re-CORD (Verb: stress 2nd)", "record_noun", "record_verb", 1, "Unit 4 Stress: Verb on 2nd beat")
            };
        }

        // ---------------------------------------------------------------------
        // 6. Unit Challenge Questions (12 Questions)
        // ---------------------------------------------------------------------
        public static List<U10ChallengeQuestion> GetChallengeQuestions()
        {
            return new List<U10ChallengeQuestion>
            {
                new U10ChallengeQuestion(1, U10ChallengeQuestionType.Text_MCQ, "The Greek root 'homo-' means...", new[] { "same", "different", "sound" }, 0, "", "U10_VO_card1", "Homo- is a prefix meaning 'same'."),
                new U10ChallengeQuestion(2, U10ChallengeQuestionType.Text_MCQ, "The root '-phone' means...", new[] { "sound", "name", "writing" }, 0, "", "U10_VO_card1", "-phone means sound, like in telephone and microphone."),
                new U10ChallengeQuestion(3, U10ChallengeQuestionType.Which_One_Fits, "Fill in: \"I can ______ the bell.\"", new[] { "hear", "here" }, 0, "hear", "hear", "Hear with your ear."),
                new U10ChallengeQuestion(4, U10ChallengeQuestionType.Which_One_Fits, "Fill in: \"______ going to be late.\"", new[] { "They're", "Their", "There" }, 0, "they're", "they're", "They're is short for 'they are'."),
                new U10ChallengeQuestion(5, U10ChallengeQuestionType.Which_One_Fits, "Fill in: \"I want to go ______.\"", new[] { "too", "to", "two" }, 0, "too", "too", "Too means 'also' or 'as well'."),
                new U10ChallengeQuestion(6, U10ChallengeQuestionType.Sound_Twins, "Which word sounds identical to 'flower'?", new[] { "flour", "floor", "flow" }, 0, "flour", "flour", "Flower and flour are homophones."),
                new U10ChallengeQuestion(7, U10ChallengeQuestionType.Two_Meanings, "In \"The dog's bark was loud\", 'bark' means:", new[] { "a dog's sound", "tree skin" }, 0, "bark", "bark", "Bark here is the sound made by a canine."),
                new U10ChallengeQuestion(8, U10ChallengeQuestionType.Say_It_Two_Ways, "In \"Please wind the clock\", 'wind' is said as:", new[] { "\"wynd\" (to turn)", "\"winned\" (air)" }, 0, "wind", "wind_turn", "Wind (wynd) means to turn or crank."),
                new U10ChallengeQuestion(9, U10ChallengeQuestionType.Text_MCQ, "Words that sound the same but have different spellings are...", new[] { "Homophones", "Homographs", "Homonyms" }, 0, "", "U10_VO_card2", "Homophones = same sound, different spelling (homo + phone)."),
                new U10ChallengeQuestion(10, U10ChallengeQuestionType.Text_MCQ, "Words spelled the same but pronounced differently are...", new[] { "Homographs", "Homophones", "Homonyms" }, 0, "", "U10_VO_card4", "Homographs = same writing/spelling, different sound (homo + graph)."),
                new U10ChallengeQuestion(11, U10ChallengeQuestionType.Say_It_Two_Ways, "In \"She broke the record\", where is the stress?", new[] { "RE-cord (1st beat)", "re-CORD (2nd beat)" }, 0, "record", "record_noun", "As a noun, the accent is on the 1st syllable: RE-cord."),
                new U10ChallengeQuestion(12, U10ChallengeQuestionType.Transfer_Stress, "Transfer: \"The soldier will desert his post.\" Where is the stress?", new[] { "de-SERT (Verb: 2nd beat)", "DE-sert (Noun: 1st beat)" }, 0, "desert", "desert_verb", "As a verb meaning to abandon, stress is on the 2nd beat: de-SERT.")
            };
        }
    }
}
