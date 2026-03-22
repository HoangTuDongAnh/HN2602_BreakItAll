using UnityEngine;

namespace _Game.Scripts.Core.Scoring
{
    public class ScoreRule
    {
        private readonly int _baseLineScore;
        private readonly int _comboBonusPerStreak;

        public ScoreRule(int baseLineScore, int comboBonusPerStreak)
        {
            _baseLineScore = baseLineScore;
            _comboBonusPerStreak = comboBonusPerStreak;
        }

        public ScoreCalculationResult CalculateLineClearScore(int totalLines, int combo)
        {
            int lineScore = totalLines * _baseLineScore;
            int comboBonus = Mathf.Max(0, combo - 1) * _comboBonusPerStreak;

            return new ScoreCalculationResult
            {
                LineScore = lineScore,
                ComboBonus = comboBonus,
                GainedScore = lineScore + comboBonus
            };
        }
    }
}