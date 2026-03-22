namespace _Game.Scripts.Core.Services
{
    public interface IAudioService
    {
        void PlayButtonClick();
        void PlayPlaceBlock();
        void PlayLineClear();
        void PlayGameOver();

        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);

        bool IsMusicEnabled { get; }
        bool IsSfxEnabled { get; }
    }
}