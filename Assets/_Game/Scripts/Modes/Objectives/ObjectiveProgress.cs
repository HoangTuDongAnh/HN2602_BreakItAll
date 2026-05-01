namespace _Game.Scripts.Modes.Objectives
{
    public readonly struct ObjectiveProgress
    {
        #region Static
        public static ObjectiveProgress Empty => new ObjectiveProgress(string.Empty, 0, 0, false, false);
        #endregion

        #region Properties
        public string DisplayText { get; }
        public int CurrentValue { get; }
        public int TargetValue { get; }
        public bool IsCompleted { get; }
        public bool IsFailed { get; }
        public float Normalized => TargetValue <= 0 ? 0f : (float)CurrentValue / TargetValue;
        #endregion

        #region Constructor
        public ObjectiveProgress(string displayText, int currentValue, int targetValue, bool isCompleted, bool isFailed)
        {
            DisplayText = displayText;
            CurrentValue = currentValue;
            TargetValue = targetValue;
            IsCompleted = isCompleted;
            IsFailed = isFailed;
        }
        #endregion
    }
}
