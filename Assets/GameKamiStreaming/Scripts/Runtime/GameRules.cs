using System;
using System.Collections.Generic;

namespace GameKamiStreaming
{
    public enum StageTimeoutOutcome
    {
        OpenSkillTree,
        GameOver
    }

    public static class StageProgressionRules
    {
        public static StageTimeoutOutcome EvaluateTimeout(StageRow stage, bool bossDefeated)
        {
            return stage != null && stage.bossId > 0 && !bossDefeated
                ? StageTimeoutOutcome.GameOver
                : StageTimeoutOutcome.OpenSkillTree;
        }
    }

    public static class SkillProgressionRules
    {
        public const string ManagerActivationSkillKey = "SD10116";
        const string FirstManagerUpgradeKey = "SD10117";
        const string LastManagerUpgradeKey = "SD10128";

        public static bool RequiresManagerActivation(SkillTreeRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.skillStringKey))
            {
                return false;
            }

            return string.CompareOrdinal(row.skillStringKey, FirstManagerUpgradeKey) >= 0 &&
                string.CompareOrdinal(row.skillStringKey, LastManagerUpgradeKey) <= 0;
        }

        public static bool HasPrerequisite(
            SkillTreeRow row,
            IReadOnlyList<SkillTreeRow> allSkills,
            Func<SkillTreeRow, int> getUpgradeCount,
            bool managerActivationComplete)
        {
            if (row == null)
            {
                return false;
            }

            if (RequiresManagerActivation(row) && !managerActivationComplete)
            {
                return false;
            }

            if (row.upgradeRank <= 1 || allSkills == null)
            {
                return true;
            }

            for (var i = 0; i < allSkills.Count; i++)
            {
                var previousRow = allSkills[i];
                if (previousRow != null &&
                    previousRow.reinforcedType == row.reinforcedType &&
                    previousRow.upgradeRank == row.upgradeRank - 1)
                {
                    return getUpgradeCount != null &&
                        getUpgradeCount(previousRow) >= GetUpgradeLimit(previousRow);
                }
            }

            return true;
        }

        public static int GetUpgradeLimit(SkillTreeRow row)
        {
            return Math.Max(1, row != null ? row.upgradeCount : 1);
        }

        public static int GetCurrentCost(int baseCost, int currentUpgradeCount)
        {
            if (baseCost <= 0)
            {
                return 0;
            }

            var clampedCount = Math.Max(0, Math.Min(30, currentUpgradeCount));
            var inflatedCost = (long)baseCost * (1L << clampedCount);
            return inflatedCost >= int.MaxValue ? int.MaxValue : (int)inflatedCost;
        }

        public static float GetStandardMultiplier(SkillTreeRow row)
        {
            return 1f + Math.Max(0f, row != null ? row.upAmount : 0f);
        }

        public static float GetRankBasedPercentMultiplier(SkillTreeRow row)
        {
            if (row == null)
            {
                return 1f;
            }

            switch (row.upgradeRank)
            {
                case 1: return 1.1f;
                case 2: return 1.3f;
                case 3: return 1.5f;
                default: return row.upAmount >= 1f ? row.upAmount : 1f + Math.Max(0f, row.upAmount);
            }
        }
    }
}
