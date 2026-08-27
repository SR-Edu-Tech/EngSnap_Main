using System;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.Common
{
    [CreateAssetMenu(fileName = "NewTopicData", menuName = "EngSnap/Topic Data", order = 1)]
    public class TopicData : ScriptableObject
    {
        [Header("Topic Identifiers")]
        [Tooltip("Unique topic or unit identifier, e.g. 'Unit1', 'Unit2', 'Unit3'.")]
        public string topicID = "Unit1";

        [Header("Reward Header Text")]
        [Tooltip("Champion title displayed on the Reward Panel (e.g. 'Alphabet Phonics Champion').")]
        public string championTitle = "Phonics Champion";

        [Tooltip("Subtitle displayed below the main title (e.g. 'UNIT COMPLETE').")]
        public string subtitle = "UNIT COMPLETE";

        [Header("Content Learned")]
        [Tooltip("List of learned words, sounds, or activities to display separated by bullet symbols.")]
        public List<string> learnedWords = new List<string> { "Meet Phonics", "Sound & Letter", "Sound Wall", "Star Round" };

        [Header("Reward Badges")]
        [Tooltip("Number of star badges to spawn on completion.")]
        public int starCount = 4;

        [Header("Audio & Voice Clips")]
        [Tooltip("Dialogue voice clip played when unit is completed (e.g. 'Unit 1 Phonics Star!').")]
        public AudioClip dialogueSound;

        /// <summary>
        /// Gets the PlayerPrefs key used to check if the reward was already shown for this topic.
        /// </summary>
        public string RewardShownPrefKey => $"{topicID}_rewardShown";
    }
}
