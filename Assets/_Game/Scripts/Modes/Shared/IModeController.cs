namespace BreakItAll.Modes
{
    public interface IModeController
    {
        bool IsRunning { get; }
        void StartMode();
        void StopMode();
    }
}