using _Game.Scripts.Data;

namespace _Game.Scripts.Core.Services
{
    public interface IGameStateService
    {
        GameState CurrentState { get; }
        void SetState(GameState state);
        bool IsInputAllowed();
    }
}