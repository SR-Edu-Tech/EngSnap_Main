using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// One introduction card, e.g. "I am Tony." + waving-kid sprite.
/// The ARRAY ORDER in IntroductionStrip_BB2.cards IS the correct tap order
/// (name → age → class → like → have → favourite).
/// </summary>
[System.Serializable]
public class IntroCardData_BB2
{
    [Tooltip("The sentence this card represents, e.g. 'I am Tony.'")]
    [TextArea] public string sentenceText;

    [Tooltip("Illustration shown on the card, e.g. waving kid / birthday cake / dog")]
    public Sprite illustrationSprite;

    [Tooltip("VO clip read aloud when placed correctly AND during the full-intro playback")]
    public AudioClip audioClip;
}
