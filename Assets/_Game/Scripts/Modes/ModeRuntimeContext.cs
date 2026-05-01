using _Game.Scripts.Core;
using _Game.Scripts.Logic;

namespace _Game.Scripts.Modes
{
    public sealed class ModeRuntimeContext
    {
        #region Properties
        public GameManager GameManager { get; }
        public ScoreManager ScoreManager { get; }
        public GameBalanceConfig Balance { get; }
        public GridManager GridManager { get; }
        public BlockSpawner BlockSpawner { get; }
        #endregion

        #region Constructor
        public ModeRuntimeContext(GameManager gameManager, ScoreManager scoreManager, GameBalanceConfig balance, GridManager gridManager, BlockSpawner blockSpawner)
        {
            GameManager = gameManager;
            ScoreManager = scoreManager;
            Balance = balance;
            GridManager = gridManager;
            BlockSpawner = blockSpawner;
        }
        #endregion
    }
}
