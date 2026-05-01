namespace _Game.Scripts.Logic.Tools
{
    /// <summary>
    /// Tool ho tro cho gameplay. Ten EndlessToolType duoc giu lai de khong lam mat reference prefab/scene cu.
    /// </summary>
    public enum EndlessToolType
    {
        None = 0,
        PlaceSingleCell = 1,
        RemoveSpawnBlock = 2,
        BombSquare = 3,

        // Compatibility: cac prefab/inspector cu neu dang dung BombCross se van compile.
        BombCross = BombSquare
    }
}
