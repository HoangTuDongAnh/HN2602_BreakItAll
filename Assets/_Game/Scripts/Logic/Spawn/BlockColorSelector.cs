using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Logic.Spawn
{
    public class BlockColorSelector
    {
        private readonly IList<Color> _palette;

        public BlockColorSelector(IList<Color> palette)
        {
            _palette = palette;
        }

        public Color GetRandomColor()
        {
            if (_palette != null && _palette.Count > 0)
            {
                Color color = _palette[Random.Range(0, _palette.Count)];

                // Giữ tương thích với palette cũ nếu alpha bị lưu thành 0
                if (color.a <= 0f)
                    color.a = 1f;

                return color;
            }

            return Color.white;
        }
    }
}