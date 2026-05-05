using UnityEngine;
using _Game.Scripts.Core.Services;
using _Game.Scripts.Core.Scoring;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Core
{
    public class ScoreManager : MonoBehaviour
    {
        #region Singleton
        public static ScoreManager Instance { get; private set; }

        private ScoreRule _scoreRule;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            LoadHighScore();
            RebuildScoreRule();
            GameServices.RegisterScore(this);
        }

        private void OnDestroy()
        {
            if (GameServices.Score == this)
                GameServices.RegisterScore(null);

            GameEvents.OnMoveCompleted -= HandleMoveCompleted;
        }
        #endregion

        #region Legacy Serialized Fields
        [Header("Legacy Score Settings")]
        [SerializeField] private int _baseLineScore = 100;
        [SerializeField] private int _comboBonusPerStreak = 50;

        [Header("Optional Placement Score")]
        [SerializeField] private int _scorePerPlacedCell = 0;
        #endregion

        #region Runtime
        private int _currentScore;
        private int _bestScore;
        private int _comboCount;
        #endregion

        #region Properties
        public int CurrentScore => _currentScore;
        public int BestScore => _bestScore;
        #endregion

        #region Lifecycle
        private void Start()
        {
            RebuildScoreRule();

            GameEvents.OnMoveCompleted += HandleMoveCompleted;
            BroadcastAll();
        }
        #endregion

        #region Service API
        public void ResetScore()
        {
            _currentScore = 0;
            _comboCount = 0;
            BroadcastAll();
            NotifyObjectiveScoreProgress();
        }

        public void AddPlacementScore(int occupiedCellCount)
        {
            if (!IsScoringEnabledForCurrentSession()) return;
            if (occupiedCellCount <= 0) return;

            int placementScorePerCell = GameServices.Balance != null
                ? GameServices.Balance.PlacementScorePerCell
                : _scorePerPlacedCell;

            if (placementScorePerCell <= 0) return;

            _currentScore += occupiedCellCount * placementScorePerCell;
            CheckHighScore();
            GameEvents.RaiseScoreChanged(_currentScore, _bestScore);
            NotifyObjectiveScoreProgress();
        }

        public void AddLineClearScore(int totalLines, int combo)
        {
            if (!IsScoringEnabledForCurrentSession()) return;
            if (totalLines <= 0) return;

            EnsureScoreRule();

            ScoreCalculationResult calc = _scoreRule.CalculateLineClearScore(totalLines, combo);
            _currentScore += calc.GainedScore;

            CheckHighScore();
            GameEvents.RaiseScoreChanged(_currentScore, _bestScore);
            NotifyObjectiveScoreProgress();
        }

        public void SaveHighScore()
        {
            GameSave.SetInt(SaveKeys.HighScore, _bestScore);
            GameSave.Save();
        }
        #endregion

        #region Event Handlers
        private void HandleMoveCompleted(int totalLines, Vector3 effectCenter)
        {
            if (!IsScoringEnabledForCurrentSession())
            {
                _comboCount = 0;
                GameEvents.RaiseComboUpdated(_comboCount);
                NotifyObjectiveScoreProgress();
                return;
            }

            if (totalLines > 0)
            {
                _comboCount++;

                EnsureScoreRule();
                ScoreCalculationResult calc = _scoreRule.CalculateLineClearScore(totalLines, _comboCount);

                _currentScore += calc.GainedScore;

                CheckHighScore();
                GameEvents.RaiseScoreChanged(_currentScore, _bestScore);
                NotifyObjectiveScoreProgress();

                GameEvents.RaiseShowFloatingText(
                    $"+{calc.GainedScore}",
                    effectCenter,
                    Color.white,
                    Mathf.Clamp(1f + (0.1f * totalLines), 1f, 1.4f)
                );

                GameEvents.RaiseComboUpdated(_comboCount);

                if (_comboCount > 1)
                {
                    GameEvents.RaiseShowFloatingText(
                        $"Combo x{_comboCount}",
                        effectCenter + new Vector3(0f, 0.5f, 0f),
                        Color.yellow,
                        1.05f
                    );
                }
            }
            else
            {
                _comboCount = 0;
                GameEvents.RaiseComboUpdated(_comboCount);
            }
        }
        #endregion

        #region Internal
        private void RebuildScoreRule()
        {
            int baseLineScore = GameServices.Balance != null
                ? GameServices.Balance.BaseLineScore
                : _baseLineScore;

            int comboBonusPerStreak = GameServices.Balance != null
                ? GameServices.Balance.ComboBonusPerStreak
                : _comboBonusPerStreak;

            _scoreRule = new ScoreRule(baseLineScore, comboBonusPerStreak);
        }

        private void EnsureScoreRule()
        {
            if (_scoreRule == null)
                RebuildScoreRule();
        }

        private void LoadHighScore()
        {
            _bestScore = GameSave.GetInt(SaveKeys.HighScore, 0);
        }

        private void CheckHighScore()
        {
            if (_currentScore <= _bestScore) return;

            _bestScore = _currentScore;
            SaveHighScore();
        }

        private void BroadcastAll()
        {
            GameEvents.RaiseScoreChanged(_currentScore, _bestScore);
            GameEvents.RaiseComboUpdated(_comboCount);
        }

        private bool IsScoringEnabledForCurrentSession()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return true;
            if (manager.CurrentModeType != GameModeType.Arcade) return true;

            LevelDefinition level = manager.ActiveArcadeLevel;
            if (level == null) return false;

            return level.UsesScore;
        }

        private void NotifyObjectiveScoreProgress()
        {
            if (GameServices.Session != null)
                GameServices.Session.ReportScoreChanged(_currentScore);
        }
        #endregion
    }
}
