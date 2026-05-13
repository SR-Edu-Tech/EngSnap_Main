using UnityEngine;
using System.Collections.Generic;

// WordTopicData_S1A — ScriptableObject that holds one word-search topic.
//
// HOW TO CREATE:
//   Right-click in Project → Create → WordSearch → Topic Data
//   Fill in topicName and the words list in the Inspector.
//
[CreateAssetMenu(menuName = "WordSearch/Topic Data", fileName = "NewWordTopic")]
public class WordTopicData_S1A : ScriptableObject
{
    [Tooltip("Display name shown on the topic selection screen, e.g. 'Days of the Week'")]
    public string topicName = "New Topic";

    [Tooltip("All words the player must find (uppercase, no spaces)")]
    public List<string> words = new List<string>();
}
