using System;
using System.Collections.Generic;

namespace GameKamiStreaming
{
    [Serializable]
    public sealed class ResourceRow
    {
        public int resourceId;
        public string resourceName;
        public string imageId;
        public string effectId;
    }

    [Serializable]
    public sealed class PieceRow
    {
        public int pieceId;
        public string pieceName;
        public int resourceId;
        public int resourceAmount;
        public int maxHp;
        public string imageId;
        public string effectId;
    }

    [Serializable]
    public sealed class StagePieceWeight
    {
        public int pieceId;
        public float weight;
    }

    [Serializable]
    public sealed class StageRow
    {
        public int stageId;
        public int bossId;
        public string effectId;
        public readonly List<StagePieceWeight> pieceWeights = new List<StagePieceWeight>();
    }

    [Serializable]
    public sealed class SkillTreeRow
    {
        public int tileId;
        public int reinforcedType;
        public int upAmount;
        public bool useSubscription;
        public int followCost;
        public int watcherCost;
        public int loveCost;
        public int donationCost;
        public int redDonationCost;
        public string effectId;
        public int unlockPieceId;
        public int unlockResourceId;
    }

    [Serializable]
    public sealed class EffectRow
    {
        public string effectId;
        public string effectName;
        public string prefabPath;
    }
}
