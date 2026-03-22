using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Logic.Placement;

namespace _Game.Scripts.Core.Services
{
    public interface IBoardQueryService
    {
        BoardState GetBoardState();
        GridCoord GetAnchorCoord(Vector3 worldPosition);
        bool CanPlaceBlockAnywhere(List<Vector2Int> shapeOffsets);
        (Vector2 min, Vector2 max) GetBoardWorldBounds();
    }
}