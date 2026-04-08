using System.Collections.Generic;

namespace BreakItAll.Gameplay
{
    /// <summary>
    /// Shape runtime tạm cho M1.
    /// M2 sẽ thay bằng Data/ShapeDefinition + repository.
    /// </summary>
    public static class DefaultShapeLibrary
    {
        public static ShapeSpawnPool CreateDefaultPool()
        {
            List<ShapeData> shapes = new List<ShapeData>
            {
                new ShapeData("single", new[]
                {
                    new CellCoord(0, 0)
                }),

                new ShapeData("line2_h", new[]
                {
                    new CellCoord(0, 0),
                    new CellCoord(1, 0)
                }),

                new ShapeData("line3_h", new[]
                {
                    new CellCoord(0, 0),
                    new CellCoord(1, 0),
                    new CellCoord(2, 0)
                }),

                new ShapeData("line2_v", new[]
                {
                    new CellCoord(0, 0),
                    new CellCoord(0, 1)
                }),

                new ShapeData("square2", new[]
                {
                    new CellCoord(0, 0),
                    new CellCoord(1, 0),
                    new CellCoord(0, 1),
                    new CellCoord(1, 1)
                }),

                new ShapeData("l3", new[]
                {
                    new CellCoord(0, 0),
                    new CellCoord(0, 1),
                    new CellCoord(1, 0)
                })
            };

            return new ShapeSpawnPool(shapes);
        }
    }
}