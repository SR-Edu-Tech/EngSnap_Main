using System;
using UnityEngine;

namespace EngSnap.ConfusedWords
{
    public enum M3A_U7_Branch
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
    /// Persists Unit 7 branch completion and progression flags independently of scene reloads.
    /// Follows the established Book 3 persistent progression architecture.
    /// </summary>
    public sealed class M3A_U7_HubProgress : MonoBehaviour
    {
        private const string KeyPrefix = "M3A_U7_Complete_";
        private const string IntroCompleteKey = "M3A_U7_IntroComplete";
        private const string PendingCompletionKey = "M3A_U7_PendingCompletion";

        private const string L01CompleteKey = "M3A_U7_L01_Complete";
        private const string L02CompleteKey = "M3A_U7_L02_Complete";
        private const string R01CompleteKey = "M3A_U7_R01_Complete";
        private const string R02CompleteKey = "M3A_U7_R02_Complete";
        private const string R03CompleteKey = "M3A_U7_R03_Complete";
        private const string W01CompleteKey = "M3A_U7_W01_Complete";
        private const string W02CompleteKey = "M3A_U7_W02_Complete";
        private const string SP01CompleteKey = "M3A_U7_SP01_Complete";
        private const string G01CompleteKey = "M3A_U7_G01_Complete";
        private const string G02CompleteKey = "M3A_U7_G02_Complete";
        private const string RP01CompleteKey = "M3A_U7_RP01_Complete";
        private const string RP02CompleteKey = "M3A_U7_RP02_Complete";
        private const string Q01CompleteKey = "M3A_U7_Q01_Complete";
        public const string RewardCompleteKey = "M3A_U7_Reward_Complete";
        public const string BadgeKey = "M3A_U7_WordDetective";
        public const string UnitCompleteKey = "M3A_U7_UnitComplete";

        private static readonly bool[] Completed = new bool[6];
        private static bool initialized;

        public static event Action<M3A_U7_Branch> BranchCompleted;
        public static event Action ProgressUpdated;

        public static bool IsListeningComplete => IsComplete(M3A_U7_Branch.Listening);
        public static bool IsReadingComplete => IsComplete(M3A_U7_Branch.Reading);
        public static bool IsWritingComplete => IsComplete(M3A_U7_Branch.Writing);
        public static bool IsSpeakingComplete => IsComplete(M3A_U7_Branch.Speaking);
        public static bool IsGameComplete => IsComplete(M3A_U7_Branch.Game);
        public static bool IsRoleplayComplete => IsComplete(M3A_U7_Branch.Roleplay);

        public static bool IsL01Complete => PlayerPrefs.GetInt(L01CompleteKey, 0) == 1;
        public static bool IsL02Complete => PlayerPrefs.GetInt(L02CompleteKey, 0) == 1;
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
        public static bool IsRewardComplete => PlayerPrefs.GetInt(RewardCompleteKey, 0) == 1;
        public static bool HasWordDetectiveBadge => PlayerPrefs.GetInt(BadgeKey, 0) == 1;
        public static bool IsUnitComplete => PlayerPrefs.GetInt(UnitCompleteKey, 0) == 1;


        public static bool AllSixBranchesComplete => CompletedCount >= 6;
        public static bool QuizUnlocked => AllSixBranchesComplete;
        public static bool IsQuizUnlocked() => QuizUnlocked;
        public static bool RewardUnlocked => IsQ01Complete;

        private void Awake()
        {
            Load();
        }

        public static bool IsComplete(M3A_U7_Branch branch)
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

        public static bool IntroCompleted => IsIntroComplete();

        public static bool IsIntroComplete()
        {
            return PlayerPrefs.GetInt(IntroCompleteKey, 0) == 1;
        }


        public static void MarkIntroComplete()
        {
            if (IsIntroComplete())
            {
                return;
            }

            PlayerPrefs.SetInt(IntroCompleteKey, 1);
            PlayerPrefs.Save();
            ProgressUpdated?.Invoke();
        }

        public static void MarkComplete(M3A_U7_Branch branch)
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
            ProgressUpdated?.Invoke();
        }

        public static void MarkL01Complete()
        {
            PlayerPrefs.SetInt(L01CompleteKey, 1);
            PlayerPrefs.Save();
            EvaluateListeningCompletion();
        }

        public static void MarkL02Complete()
        {
            PlayerPrefs.SetInt(L02CompleteKey, 1);
            PlayerPrefs.Save();
            EvaluateListeningCompletion();
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
            MarkComplete(M3A_U7_Branch.Speaking);
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

        public static void EvaluateListeningCompletion()
        {
            if (IsL01Complete && IsL02Complete)
            {
                MarkComplete(M3A_U7_Branch.Listening);
            }
        }

        public static void EvaluateReadingCompletion()
        {
            if (IsR01Complete && IsR02Complete && IsR03Complete)
            {
                MarkComplete(M3A_U7_Branch.Reading);
            }
        }

        public static void EvaluateWritingCompletion()
        {
            if (IsW01Complete && IsW02Complete)
            {
                MarkComplete(M3A_U7_Branch.Writing);
            }
        }

        public static void EvaluateGameCompletion()
        {
            if (IsG01Complete && IsG02Complete)
            {
                MarkComplete(M3A_U7_Branch.Game);
            }
        }

        public static void EvaluateRoleplayCompletion()
        {
            if (IsRP01Complete && IsRP02Complete)
            {
                MarkComplete(M3A_U7_Branch.Roleplay);
            }
        }

        public static void MarkQ01Complete(int score = 12, int passThreshold = 9)
        {
            if (score >= passThreshold)
            {
                PlayerPrefs.SetInt(Q01CompleteKey, 1);
                PlayerPrefs.Save();
                ProgressUpdated?.Invoke();
            }
        }

        public static void AwardWordDetectiveBadge()
        {
            PlayerPrefs.SetInt(BadgeKey, 1);
            PlayerPrefs.Save();
            ProgressUpdated?.Invoke();
        }

        public static void MarkRewardComplete()
        {
            PlayerPrefs.SetInt(RewardCompleteKey, 1);
            PlayerPrefs.Save();
            AwardWordDetectiveBadge();
            MarkUnitComplete();
        }

        public static void MarkUnitComplete()
        {
            PlayerPrefs.SetInt(UnitCompleteKey, 1);
            PlayerPrefs.Save();
            ProgressUpdated?.Invoke();
        }

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

        public static void ResetProgress()
        {
            for (int i = 0; i < Completed.Length; i++)
            {
                Completed[i] = false;
                PlayerPrefs.DeleteKey(KeyPrefix + (M3A_U7_Branch)i);
            }

            PlayerPrefs.DeleteKey(IntroCompleteKey);
            PlayerPrefs.DeleteKey(PendingCompletionKey);
            PlayerPrefs.DeleteKey(L01CompleteKey);
            PlayerPrefs.DeleteKey(L02CompleteKey);
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
            PlayerPrefs.DeleteKey(RewardCompleteKey);
            PlayerPrefs.DeleteKey(BadgeKey);
            PlayerPrefs.DeleteKey(UnitCompleteKey);
            PlayerPrefs.Save();

            initialized = false;

            Load();
            ProgressUpdated?.Invoke();
        }

        private static void Load()
        {
            if (initialized)
            {
                return;
            }

            for (int i = 0; i < Completed.Length; i++)
            {
                Completed[i] = PlayerPrefs.GetInt(KeyPrefix + (M3A_U7_Branch)i, 0) == 1;
            }

            initialized = true;
        }
    }
}
