using BreakItAll.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BreakItAll.UI
{
    public sealed class BoardVisualController : MonoBehaviour
    {
        [SerializeField] private RectTransform boardContainer;
        [SerializeField] private Image cellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(48f, 48f);
        [SerializeField] private float spacing = 4f;
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color filledColor = Color.green;

        private Image[,] _cellViews;
        private BoardState _boundBoardState;

        public void Bind(BoardState boardState)
        {
            if (_boundBoardState != null)
            {
                _boundBoardState.StateChanged -= Refresh;
            }

            _boundBoardState = boardState;

            if (_boundBoardState == null)
            {
                return;
            }

            BuildGrid(_boundBoardState.Width, _boundBoardState.Height);
            _boundBoardState.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_boundBoardState != null)
            {
                _boundBoardState.StateChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (_boundBoardState == null || _cellViews == null)
            {
                return;
            }

            for (int x = 0; x < _boundBoardState.Width; x++)
            {
                for (int y = 0; y < _boundBoardState.Height; y++)
                {
                    bool occupied = _boundBoardState.IsOccupied(new CellCoord(x, y));
                    _cellViews[x, y].color = occupied ? filledColor : emptyColor;
                }
            }
        }

        private void BuildGrid(int width, int height)
        {
            ClearChildren();

            _cellViews = new Image[width, height];

            if (boardContainer == null || cellPrefab == null)
            {
                return;
            }

            float totalWidth = width * cellSize.x + (width - 1) * spacing;
            float totalHeight = height * cellSize.y + (height - 1) * spacing;
            boardContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Image cell = Instantiate(cellPrefab, boardContainer);
                    RectTransform rect = cell.rectTransform;
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    rect.sizeDelta = cellSize;

                    float posX = x * (cellSize.x + spacing);
                    float posY = y * (cellSize.y + spacing);
                    rect.anchoredPosition = new Vector2(posX, posY);

                    _cellViews[x, y] = cell;
                }
            }
        }

        private void ClearChildren()
        {
            if (boardContainer == null)
            {
                return;
            }

            for (int i = boardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(boardContainer.GetChild(i).gameObject);
            }
        }
    }
}