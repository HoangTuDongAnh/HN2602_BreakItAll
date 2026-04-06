namespace BreakItAll.Core
{
    /// <summary>
    /// Điều phối session gameplay hiện tại.
    /// M1.1 chỉ giữ vai trò placeholder rõ trách nhiệm.
    /// Logic chi tiết sẽ triển khai ở các bước sau.
    /// </summary>
    public class GameSessionCoordinator
    {
        public bool HasActiveSession { get; private set; }

        public void StartSession()
        {
            HasActiveSession = true;
        }

        public void EndSession()
        {
            HasActiveSession = false;
        }
    }
}