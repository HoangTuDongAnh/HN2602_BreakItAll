using System;
using System.Collections.Generic;
using _Game.Scripts.Data;

namespace _Game.Scripts.Logic
{
    [Serializable]
    public class SpawnDebugCandidate
    {
        public string ShapeId;
        public string DisplayName;
        public BlockTier Tier;
        public int RotationIndex;
        public float Weight;
        public int FitCount;
        public int ClearPotential;
        public int CellCount;
        public bool Selected;
        public string Reason;
    }

    [Serializable]
    public class SpawnDebugReport
    {
        public int RequestedCount;
        public float FillRate;
        public int RawPoolCount;
        public int FilteredPoolCount;
        public int CandidateCount;
        public bool ForcePlayable;
        public string Summary;
        public readonly List<SpawnDebugCandidate> Candidates = new List<SpawnDebugCandidate>();
        public readonly List<SpawnDebugCandidate> Selected = new List<SpawnDebugCandidate>();

        public void Clear()
        {
            RequestedCount = 0;
            FillRate = 0f;
            RawPoolCount = 0;
            FilteredPoolCount = 0;
            CandidateCount = 0;
            ForcePlayable = false;
            Summary = string.Empty;
            Candidates.Clear();
            Selected.Clear();
        }
    }
}
