using _Game.Scripts.Data;
using _Game.Scripts.Logic;
using UnityEngine;

namespace _Game.Scripts.View
{
    /// <summary>
    /// Quản lý hiển thị của một ô đơn lẻ.
    /// </summary>
    public class GridCellView : MonoBehaviour
    {
        #region Components
        [Header("References")]
        [Tooltip("SpriteRenderer dùng để hiển thị màu nền của ô")]
        [SerializeField] private SpriteRenderer _renderer; 
        
        [Tooltip("Text dùng để debug tọa độ (Optional)")]
        [SerializeField] private TextMesh _debugText; 
        #endregion

        // Màu của bóng (Trắng mờ 50%)
        private readonly Color _shadowColor = new Color(1f, 1f, 1f, 0.5f);

        private GridCell _linkedData;
        
        // [NEW] Màu chủ đạo được truyền vào từ BlockSpawner
        private Color _themeColor = Color.white; 

        /// <summary>
        /// Khởi tạo hiển thị. [UPDATE] Thêm tham số themeColor
        /// </summary>
        public void Init(GridCell data, Color themeColor)
        {
            _linkedData = data;
            _themeColor = themeColor; // Lưu màu lại
            
            gameObject.name = $"Cell [{data.x},{data.y}]";
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (_linkedData == null || _renderer == null) return;

            // --- 1. Xử lý ô trống ---
            if (!_linkedData.IsOccupied)
            {
                _renderer.color = new Color(0.2f, 0.2f, 0.2f, 1f); 
                return;
            }

            // --- 2. Xử lý ô có gạch (Color Coding) ---
            switch (_linkedData.Type)
            {
                case CellType.Normal:
                    // [UPDATE] Sử dụng màu theme thay vì màu trắng mặc định
                    _renderer.color = _themeColor;
                    break;
                case CellType.Boom:
                    _renderer.color = Color.red; 
                    break;
                case CellType.Tool:
                    _renderer.color = Color.yellow; 
                    break;
                case CellType.Ice:
                    _renderer.color = Color.cyan; 
                    break;
                default:
                    _renderer.color = _themeColor;
                    break;
            }
        }

        public void ShowShadowState()
        {
            if (_renderer != null)
            {
                _renderer.color = _shadowColor;
            }
        }
    }
}