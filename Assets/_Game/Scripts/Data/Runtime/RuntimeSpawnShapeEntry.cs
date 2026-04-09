using BreakItAll.Gameplay;

namespace BreakItAll.Data
{
    public sealed class RuntimeSpawnShapeEntry
    {
        public string ShapeId { get; }
        public ShapeData RuntimeShape { get; }
        public int FinalWeight { get; }
        public int DifficultyTier { get; }

        public RuntimeSpawnShapeEntry(string shapeId, ShapeData runtimeShape, int finalWeight, int difficultyTier)
        {
            ShapeId = shapeId;
            RuntimeShape = runtimeShape;
            FinalWeight = finalWeight;
            DifficultyTier = difficultyTier;
        }
    }
}