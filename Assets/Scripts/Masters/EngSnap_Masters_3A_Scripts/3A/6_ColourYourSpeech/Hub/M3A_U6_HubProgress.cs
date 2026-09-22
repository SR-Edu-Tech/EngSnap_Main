using System;
using UnityEngine;

public enum M3A_U6_Branch
{
    Listening,
    Reading,
    Writing,
    Speaking,
    Game,
    Roleplay,
    RolePlay = Roleplay
}

/// <summary>
/// Persists Unit 6 branch completion independently from temporary lesson objects.
/// </summary>
public sealed class M3A_U6_HubProgress : MonoBehaviour
{
    private const string KeyPrefix = "M3A_U6_Complete_";
    private const string IntroCompleteKey = "M3A_U6_IntroComplete";
    private const string PendingCompletionKey = "M3A_U6_PendingCompletion";

    private const string R01CompleteKey = "M3A_U6_R01_Complete";
    private const string R02CompleteKey = "M3A_U6_R02_Complete";
    private const string R03CompleteKey = "M3A_U6_R03_Complete";
    private const string W01CompleteKey = "M3A_U6_W01_Complete";
    private const string W02CompleteKey = "M3A_U6_W02_Complete";
    private const string SP01CompleteKey = "M3A_U6_SP01_Complete";
    private const string G01CompleteKey = "M3A_U6_G01_Complete";
    private const string G02CompleteKey = "M3A_U6_G02_Complete";
    private const string RP01CompleteKey = "M3A_U6_RP01_Complete";
    private const string RP02CompleteKey = "M3A_U6_RP02_Complete";
    private const string Q01CompleteKey = "M3A_U6_Q01_Complete";
    private const string BadgeKey = "M3A_U6_IdiomArtist";
    private const string UnitCompleteKey = "M3A_U6_UnitComplete";
    private static readonly bool[] Completed = new bool[6];
    private static bool initialized;

    public static event Action<M3A_U6_Branch> BranchCompleted;

    public static bool IsListeningComplete => IsComplete(M3A_U6_Branch.Listening);
    public static bool IsReadingComplete => IsComplete(M3A_U6_Branch.Reading);
    public static bool IsWritingComplete => IsComplete(M3A_U6_Branch.Writing);
    public static bool IsSpeakingComplete => IsComplete(M3A_U6_Branch.Speaking);
    public static bool IsGameComplete => IsComplete(M3A_U6_Branch.Game);
    public static bool IsRoleplayComplete => IsComplete(M3A_U6_Branch.Roleplay);

    public static bool IsR01Complete => PlayerPrefs.GetInt(R01CompleteKey, 0) == 1;
    public static bool IsR02Complete => PlayerPrefs.GetInt(R02CompleteKey, 0) == 1;
    public static bool IsR03Complete => PlayerPrefs.GetInt(R03CompleteKey, 0) == 1;
    public static bool IsW01Complete => PlayerPrefs.GetInt(W01CompleteKey, 0) == 1;
    public static bool IsW02Complete => PlayerPrefs.GetInt(W02CompleteKey, 0) == 1;
    public static bool IsSP01Complete => PlayerPrefs.GetInt(SP01CompleteKey, 0) == 1;
    public static bool IsG01Complete => PlayerPrefs.GetInt(G01CompleteKey, 0) == 1;
    public static bool IsG02Complete => PlayerPrefs.GetInt(G02CompleteKey, 0) == 1;
    public static bool IsRP01Complete => PlayerPrefs.GetInt(RP01CompleteKey, 0) == 1;
    public static bool IsRP02Complete => PlayerPrefs.GetInt(RP02CompleteKey, 0) == 1;
    public static bool IsQ01Complete => PlayerPrefs.GetInt(Q01CompleteKey, 0) == 1;
    public static bool HasIdiomArtistBadge => PlayerPrefs.GetInt(BadgeKey, 0) == 1;
    public static bool IsUnitComplete => PlayerPrefs.GetInt(UnitCompleteKey, 0) == 1;

    public static void MarkQ01Complete()
    {
        PlayerPrefs.SetInt(Q01CompleteKey, 1);
        PlayerPrefs.Save();
    }

    public static void AwardIdiomArtistBadge()
    {
        PlayerPrefs.SetInt(BadgeKey, 1);
        PlayerPrefs.Save();
    }

    public static void MarkUnitComplete()
    {
        PlayerPrefs.SetInt(UnitCompleteKey, 1);
        PlayerPrefs.Save();
    }

    public static void MarkR01Complete()
    {
        PlayerPrefs.SetInt(R01CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateReadingCompletion();
    }

    public static void MarkR02Complete()
    {
        PlayerPrefs.SetInt(R02CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateReadingCompletion();
    }

    public static void MarkR03Complete()
    {
        PlayerPrefs.SetInt(R03CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateReadingCompletion();
    }

    public static void MarkW01Complete()
    {
        PlayerPrefs.SetInt(W01CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateWritingCompletion();
    }

    public static void MarkW02Complete()
    {
        PlayerPrefs.SetInt(W02CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateWritingCompletion();
    }

    public static void MarkSP01Complete()
    {
        PlayerPrefs.SetInt(SP01CompleteKey, 1);
        PlayerPrefs.Save();
        MarkComplete(M3A_U6_Branch.Speaking);
    }

    public static void MarkG01Complete()
    {
        PlayerPrefs.SetInt(G01CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateGameCompletion();
    }

    public static void MarkG02Complete()
    {
        PlayerPrefs.SetInt(G02CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateGameCompletion();
    }

    public static void MarkRP01Complete()
    {
        PlayerPrefs.SetInt(RP01CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateRoleplayCompletion();
    }

    public static void MarkRP02Complete()
    {
        PlayerPrefs.SetInt(RP02CompleteKey, 1);
        PlayerPrefs.Save();
        EvaluateRoleplayCompletion();
    }

    public static void EvaluateReadingCompletion()
    {
        if (IsR01Complete && IsR02Complete && IsR03Complete)
        {
            MarkComplete(M3A_U6_Branch.Reading);
        }
    }

    public static void EvaluateWritingCompletion()
    {
        if (IsW01Complete && IsW02Complete)
        {
            MarkComplete(M3A_U6_Branch.Writing);
        }
    }

    public static void EvaluateGameCompletion()
    {
        if (IsG01Complete && IsG02Complete)
        {
            MarkComplete(M3A_U6_Branch.Game);
        }
    }

    public static void EvaluateRoleplayCompletion()
    {
        if (IsRP01Complete && IsRP02Complete)
        {
            MarkComplete(M3A_U6_Branch.Roleplay);
        }
    }

    /// <summary>
    /// Gates the final Quiz / Gallery when all 6 branches are completed.
    /// </summary>
    public static bool QuizUnlocked => CompletedCount >= 6;
    public static bool IsQuizUnlocked() => QuizUnlocked;

    private void Awake()
    {
        Load();
    }

    public static bool IsComplete(M3A_U6_Branch branch)
    {
        Load();
        return Completed[(int)branch];
    }

    public static int CompletedCount
    {
        get
        {
            Load();
            int count = 0;
            for (int i = 0; i < Completed.Length; i++)
            {
                if (Completed[i])
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Returns whether the Unit 6 introduction has been completed.
    /// </summary>
    public static bool IsIntroComplete()
    {
        return PlayerPrefs.GetInt(IntroCompleteKey, 0) == 1;
    }

    /// <summary>
    /// Persists completion of the Unit 6 introduction.
    /// </summary>
    public static void MarkIntroComplete()
    {
        if (IsIntroComplete())
        {
            return;
        }

        PlayerPrefs.SetInt(IntroCompleteKey, 1);
        PlayerPrefs.Save();
    }


    /// <summary>
    /// Marks a Unit 6 branch complete and persists the change for later scenes and sessions.
    /// </summary>
    public static void MarkComplete(M3A_U6_Branch branch)
    {
        Load();
        int index = (int)branch;
        if (Completed[index])
        {
            return;
        }

        Completed[index] = true;
        PlayerPrefs.SetInt(KeyPrefix + branch, 1);
        PlayerPrefs.SetInt(PendingCompletionKey, 1);
        PlayerPrefs.Save();
        BranchCompleted?.Invoke(branch);
    }

    /// <summary>
    /// Consumes the one-shot completion notification used when returning from a branch scene.
    /// </summary>
    public static bool ConsumePendingCompletion()
    {
        bool pending = PlayerPrefs.GetInt(PendingCompletionKey, 0) == 1;
        if (pending)
        {
            PlayerPrefs.SetInt(PendingCompletionKey, 0);
            PlayerPrefs.Save();
        }

        return pending;
    }

    /// <summary>
    /// Clears Unit 6 progress for development and QA testing.
    /// </summary>
    public static void ResetProgress()
    {
        for (int i = 0; i < Completed.Length; i++)
        {
            Completed[i] = false;
            PlayerPrefs.DeleteKey(KeyPrefix + (M3A_U6_Branch)i);
        }

        PlayerPrefs.DeleteKey(IntroCompleteKey);
        PlayerPrefs.DeleteKey(R01CompleteKey);
        PlayerPrefs.DeleteKey(R02CompleteKey);
        PlayerPrefs.DeleteKey(R03CompleteKey);
        PlayerPrefs.DeleteKey(W01CompleteKey);
        PlayerPrefs.DeleteKey(W02CompleteKey);
        PlayerPrefs.DeleteKey(SP01CompleteKey);
        PlayerPrefs.DeleteKey(G01CompleteKey);
        PlayerPrefs.DeleteKey(G02CompleteKey);
        PlayerPrefs.DeleteKey(RP01CompleteKey);
        PlayerPrefs.DeleteKey(RP02CompleteKey);
        PlayerPrefs.DeleteKey(Q01CompleteKey);
        PlayerPrefs.DeleteKey(BadgeKey);
        PlayerPrefs.DeleteKey(UnitCompleteKey);
        PlayerPrefs.Save();
    }

    private static void Load()
    {
        if (initialized)
        {
            return;
        }

        for (int i = 0; i < Completed.Length; i++)
        {
            Completed[i] = PlayerPrefs.GetInt(KeyPrefix + (M3A_U6_Branch)i, 0) == 1;
        }

        initialized = true;
    }
}
