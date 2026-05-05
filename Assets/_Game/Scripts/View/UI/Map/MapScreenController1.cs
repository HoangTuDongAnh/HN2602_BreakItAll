using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Modes.Map;
using _Game.Scripts.View.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Map
{
    /// <summary>
    /// Scrollable Arcade level map. The map is a single vertical road, not a world selector.
    /// </summary>
    public class MapScreenController : MonoBehaviour
    {
        #region Inspector - Data
        [Header("Data")]
        [SerializeField] private MapDefinition _mapDefinition;
        #endregion

        #region Inspector - UI References
        [Header("UI References")]
        [SerializeField] private RectTransform _nodesContainer;
        [SerializeField] private RectTransform _linesContainer;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private LevelNodeView _nodePrefab;
        [SerializeField] private MapPathLineView _linePrefab;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private Button _backToHomeButton;
        [SerializeField] private bool _useLevelPreview = true;
        #endregion

        #region Inspector - Layout
        [Header("Scrollable Map Layout")]
        [SerializeField] private bool _useAutoZigzagLayout = true;
        [SerializeField] private Vector2 _firstNodePosition = Vector2.zero;
        [SerializeField] private float _verticalSpacing = 185f;
        [SerializeField] private float _zigzagAmplitude = 124f;
        [SerializeField] private int _nodesPerCurve = 5;
        [SerializeField] private bool _startFromLeft;
        [SerializeField] private float _contentTopPadding = 360f;
        [SerializeField] private float _contentBottomPadding = 300f;
        [SerializeField] private float _contentMinHeight = 2200f;
        [SerializeField] private Vector2 _nodeSize = new Vector2(156f, 156f);
        [SerializeField] private bool _focusCurrentNodeOnOpen = true;
        #endregion

        #region Inspector - Path Style
        [Header("Path Style")]
        [SerializeField] private bool _showPathLines;
        [SerializeField] private float _pathThickness = 16f;
        [SerializeField] private float _pathShadowExtraThickness = 12f;
        [SerializeField] private Color _pathShadowColor = new Color(0.04f, 0.15f, 0.34f, 0.55f);
        [SerializeField] private Color _lockedPathColor = new Color(0.2f, 0.27f, 0.38f, 0.62f);
        [SerializeField] private Color _unlockedPathColor = new Color(0.78f, 0.95f, 1f, 0.95f);
        [SerializeField] private Color _completedPathColor = new Color(1f, 0.75f, 0.16f, 0.95f);
        [SerializeField] private bool _useCurvedPath = true;
        [SerializeField] private float _pathCurveStrength = 0.46f;
        [SerializeField] private int _pathSegmentCount = 48;
        [SerializeField] private float _pathNodeRadiusOffset = 82f;
        #endregion

        #region Inspector - Style
        [Header("Map Style")]
        [SerializeField] private string _mapTitle = "ARCADE MAP";
        [SerializeField] private string _mapSubtitle = "Choose a level";
        [SerializeField] private Color _backgroundColor = new Color(0.02f, 0.44f, 0.86f, 1f);
        [SerializeField] private Color _scrollPanelColor = new Color(0.04f, 0.5f, 0.95f, 0.35f);
        [SerializeField] private bool _showDecorativeBlocks = true;
        [SerializeField] private Color _decorationColor = new Color(0.7f, 0.95f, 1f, 0.16f);
        [SerializeField] private float _decorationCellSize = 42f;
        #endregion

        #region Runtime
        private readonly Dictionary<string, LevelNodeView> _nodeViews = new Dictionary<string, LevelNodeView>();
        private readonly Dictionary<string, Vector2> _nodePositions = new Dictionary<string, Vector2>();
        private RectTransform _contentRect;
        private float _contentHeight;
        private Coroutine _focusRoutine;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheOptionalReferences();
            WireButtons();
        }

        private void OnEnable()
        {
            CacheOptionalReferences();
            WireButtons();
            Rebuild();
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                CacheOptionalReferences();
                UnityEditor.EditorUtility.SetDirty(this);
            };
        }
#endif
        #endregion

        #region Public API
        public void Rebuild()
        {
            if (_mapDefinition == null || _nodesContainer == null || _nodePrefab == null)
                return;

            CacheOptionalReferences();
            ApplyScreenStyle();
            EnsureLineContainer();
            PrepareContainers();
            ClearChildren(_nodesContainer);
            ClearChildren(_linesContainer);
            _nodeViews.Clear();
            _nodePositions.Clear();

            RefreshHeader();
            ResizeContentIfNeeded();
            BuildNodePositions();
            BuildDecorations();
            BuildLines();
            BuildNodes();
            FocusCurrentNode();
        }

        public void PlayNode(MapLevelNodeDefinition node)
        {
            if (node == null || node.Level == null) return;
            if (!ArcadeProgressService.IsNodeUnlocked(_mapDefinition, node)) return;

            if (_useLevelPreview && UIManager.Instance != null)
            {
                UIManager.Instance.ShowLevelPreview(_mapDefinition, node);
                return;
            }

            ArcadeSession.SelectLevel(_mapDefinition, node);
            GameManager.Instance?.StartArcadeLevel(node.Level);
        }

        public void BackToHome()
        {
            GameManager.Instance?.ReturnToHome();
        }
        #endregion

        #region Build
        private void RefreshHeader()
        {
            if (_titleText != null)
            {
                _titleText.text = _mapTitle;
                ApplyTitlePolish(_titleText, 0.14f);
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = _mapSubtitle;
                ApplyTitlePolish(_subtitleText, 0.1f);
            }
        }

        private void BuildNodePositions()
        {
            if (_mapDefinition == null || _mapDefinition.Nodes == null)
                return;

            for (int i = 0; i < _mapDefinition.Nodes.Count; i++)
            {
                MapLevelNodeDefinition node = _mapDefinition.Nodes[i];
                if (node == null || node.Level == null) continue;

                Vector2 position = ShouldUseAuthoredPosition(node)
                    ? node.AnchoredPosition
                    : CalculateZigzagPosition(i);

                _nodePositions[node.NodeId] = position;
            }
        }

        private void BuildLines()
        {
            if (!_showPathLines || _linesContainer == null || _mapDefinition == null || _mapDefinition.Nodes == null) return;

            for (int i = 0; i < _mapDefinition.Nodes.Count; i++)
            {
                MapLevelNodeDefinition node = _mapDefinition.Nodes[i];
                if (node == null || node.Level == null) continue;

                if (node.RequiredCompletedNodeIds != null && node.RequiredCompletedNodeIds.Count > 0)
                {
                    for (int r = 0; r < node.RequiredCompletedNodeIds.Count; r++)
                        TryCreateLine(node.RequiredCompletedNodeIds[r], node.NodeId);
                }
                else if (i > 0)
                {
                    MapLevelNodeDefinition previousNode = _mapDefinition.Nodes[i - 1];
                    if (previousNode != null)
                        TryCreateLine(previousNode.NodeId, node.NodeId);
                }
            }
        }

        private void BuildNodes()
        {
            if (_mapDefinition == null || _mapDefinition.Nodes == null)
                return;

            for (int i = 0; i < _mapDefinition.Nodes.Count; i++)
            {
                MapLevelNodeDefinition node = _mapDefinition.Nodes[i];
                if (node == null || node.Level == null) continue;

                LevelNodeView view = Instantiate(_nodePrefab, _nodesContainer);
                RectTransform rect = view.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = _nodeSize;
                    rect.anchoredPosition = _nodePositions[node.NodeId];
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                }

                bool unlocked = ArcadeProgressService.IsNodeUnlocked(_mapDefinition, node);
                bool passed = ArcadeProgressService.IsLevelPassed(node.Level);
                view.Setup(this, node, unlocked, passed, i + 1);

                _nodeViews[node.NodeId] = view;
            }
        }
        #endregion

        #region Lines
        private void TryCreateLine(string fromNodeId, string toNodeId)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId)) return;
            if (!_nodePositions.TryGetValue(fromNodeId, out Vector2 from)) return;
            if (!_nodePositions.TryGetValue(toNodeId, out Vector2 to)) return;

            MapLevelNodeDefinition fromNode = _mapDefinition.FindNode(fromNodeId);
            MapLevelNodeDefinition toNode = _mapDefinition.FindNode(toNodeId);

            bool fromCompleted = fromNode != null && ArcadeProgressService.IsLevelPassed(fromNode.Level);
            bool toUnlocked = toNode != null && ArcadeProgressService.IsNodeUnlocked(_mapDefinition, toNode);
            Color color = fromCompleted ? _completedPathColor : (toUnlocked ? _unlockedPathColor : _lockedPathColor);

            CreatePathLine(from, to, _pathShadowColor, _pathThickness + _pathShadowExtraThickness);
            CreatePathLine(from, to, color, _pathThickness);
        }

        private void CreatePathLine(Vector2 from, Vector2 to, Color color, float thickness)
        {
            MapPathLineView line = CreateLineView();
            float curveDirection = GetCurveDirection(from, to);
            if (_useCurvedPath)
                line.SetupCurved(from, to, color, thickness, _pathCurveStrength, _pathSegmentCount, _pathNodeRadiusOffset, curveDirection);
            else
                line.Setup(from, to, color, thickness);
        }

        private MapPathLineView CreateLineView()
        {
            MapPathLineView line;
            if (_linePrefab != null)
            {
                line = Instantiate(_linePrefab, _linesContainer);
            }
            else
            {
                GameObject lineObject = new GameObject("Path_Line", typeof(RectTransform), typeof(Image), typeof(MapPathLineView));
                lineObject.transform.SetParent(_linesContainer, false);
                line = lineObject.GetComponent<MapPathLineView>();
            }

            RectTransform rect = line.transform as RectTransform;
            StretchContainer(rect);
            return line;
        }
        #endregion

        #region Layout Helpers
        private bool ShouldUseAuthoredPosition(MapLevelNodeDefinition node)
        {
            return !_useAutoZigzagLayout && node.AnchoredPosition != Vector2.zero;
        }

        private Vector2 CalculateZigzagPosition(int index)
        {
            int nodesPerCurve = Mathf.Max(2, _nodesPerCurve);
            float phase = (index / (float)(nodesPerCurve - 1)) * Mathf.PI;
            float side = Mathf.Sin(phase);
            if (_startFromLeft)
                side *= -1f;

            float x = _firstNodePosition.x + side * _zigzagAmplitude;

            // Child nodes are centered inside a stretched container, so anchoredPosition.y
            // uses the content center as its origin. The old formula used -_contentHeight
            // as if the origin was at the top/bottom edge, which pushed the first nodes
            // below the visible area. Lay levels out from top to bottom instead: level 1
            // starts near the top of the scroll content, and later levels continue downward.
            float y = _contentHeight * 0.5f
                - _contentTopPadding
                + _firstNodePosition.y
                - index * Mathf.Abs(_verticalSpacing);

            return new Vector2(x, y);
        }

        private float GetCurveDirection(Vector2 from, Vector2 to)
        {
            float middleX = (from.x + to.x) * 0.5f;
            if (Mathf.Abs(middleX) > 0.01f)
                return middleX > 0f ? -1f : 1f;

            return to.x >= from.x ? -1f : 1f;
        }

        private void ResizeContentIfNeeded()
        {
            _contentRect = _nodesContainer != null ? _nodesContainer.parent as RectTransform : null;
            if (_contentRect == null)
                _contentRect = _nodesContainer;

            int count = _mapDefinition != null && _mapDefinition.Nodes != null ? _mapDefinition.Nodes.Count : 0;
            float neededHeight = _contentTopPadding + Mathf.Max(0, count - 1) * Mathf.Abs(_verticalSpacing) + _contentBottomPadding;
            _contentHeight = Mathf.Max(_contentMinHeight, neededHeight);

            _contentRect.anchorMin = new Vector2(0f, 1f);
            _contentRect.anchorMax = new Vector2(1f, 1f);
            _contentRect.pivot = new Vector2(0.5f, 1f);
            _contentRect.anchoredPosition = Vector2.zero;
            _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, _contentHeight);
        }

        private void PrepareContainers()
        {
            StretchContainer(_linesContainer);
            StretchContainer(_nodesContainer);
            if (_linesContainer != null)
                _linesContainer.SetAsFirstSibling();
        }

        private void EnsureLineContainer()
        {
            if (_linesContainer != null) return;

            GameObject lineRoot = new GameObject("Path_Lines", typeof(RectTransform));
            lineRoot.transform.SetParent(_nodesContainer.parent != null ? _nodesContainer.parent : _nodesContainer, false);
            _linesContainer = lineRoot.transform as RectTransform;
            _linesContainer.SetAsFirstSibling();
        }

        private void StretchContainer(RectTransform rect)
        {
            if (rect == null) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
        }
        #endregion

        #region Style Helpers
        private void CacheOptionalReferences()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            if (_scrollRect == null)
                _scrollRect = GetComponentInChildren<ScrollRect>(true);

            if (_backToHomeButton == null)
                _backToHomeButton = FindButton("Button_BackToMenu") ?? FindButton("Button_BackHome") ?? FindButton("Button_Home");
        }

        private void WireButtons()
        {
            if (_backToHomeButton == null) return;

            _backToHomeButton.onClick.RemoveListener(BackToHome);
            _backToHomeButton.onClick.AddListener(BackToHome);
        }

        private Button FindButton(string objectName)
        {
            Transform child = FindChild(transform, objectName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName)) return null;
            if (root.name == objectName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), objectName);
                if (result != null) return result;
            }

            return null;
        }

        private void ApplyScreenStyle()
        {
            if (_backgroundImage != null)
                _backgroundImage.color = _backgroundColor;

            if (_scrollRect == null)
                return;

            Image scrollImage = _scrollRect.GetComponent<Image>();
            if (scrollImage != null)
                scrollImage.color = _scrollPanelColor;

            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.scrollSensitivity = 26f;
        }

        private void BuildDecorations()
        {
            if (!_showDecorativeBlocks || _linesContainer == null)
                return;

            Vector2Int[][] patterns =
            {
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, -1) },
                new[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1) },
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(2, -1) }
            };

            int nodeCount = _mapDefinition != null && _mapDefinition.Nodes != null ? _mapDefinition.Nodes.Count : 0;
            int decorationCount = Mathf.Clamp(nodeCount + 2, 4, 9);
            float usableHeight = Mathf.Max(1f, _contentHeight - _contentTopPadding - _contentBottomPadding);

            for (int i = 0; i < decorationCount; i++)
            {
                float t = decorationCount <= 1 ? 0f : i / (float)(decorationCount - 1);
                // Decorations share the same centered coordinate space as nodes.
                float y = _contentHeight * 0.5f - _contentTopPadding - usableHeight * t;
                float x = ((i & 1) == 0 ? -1f : 1f) * (_zigzagAmplitude + 92f);
                float scale = i % 3 == 0 ? 0.78f : 0.62f;

                CreateDecoration($"Decor_Block_{i + 1:00}", new Vector2(x, y), patterns[i % patterns.Length], _decorationCellSize * scale);
            }
        }

        private void CreateDecoration(string name, Vector2 origin, Vector2Int[] cells, float cellSize)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(_linesContainer, false);

            RectTransform rect = root.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = origin;
            rect.sizeDelta = Vector2.zero;

            for (int i = 0; i < cells.Length; i++)
            {
                GameObject cell = new GameObject("Decor_Cell", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(root.transform, false);

                RectTransform cellRect = cell.transform as RectTransform;
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.anchoredPosition = new Vector2(cells[i].x * cellSize, cells[i].y * cellSize);
                cellRect.sizeDelta = new Vector2(cellSize - 4f, cellSize - 4f);

                Image image = cell.GetComponent<Image>();
                image.color = _decorationColor;
                image.raycastTarget = false;
            }
        }
        #endregion

        #region Focus Helpers
        private void FocusCurrentNode()
        {
            if (!_focusCurrentNodeOnOpen || _scrollRect == null)
                return;

            if (_focusRoutine != null)
                StopCoroutine(_focusRoutine);

            _focusRoutine = StartCoroutine(FocusCurrentNodeRoutine());
        }

        private IEnumerator FocusCurrentNodeRoutine()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            MapLevelNodeDefinition target = FindCurrentNode();
            if (target == null || !_nodePositions.TryGetValue(target.NodeId, out Vector2 position))
                yield break;

            RectTransform viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.transform as RectTransform;
            float viewportHeight = viewport != null ? viewport.rect.height : 0f;
            float scrollableHeight = Mathf.Max(1f, _contentHeight - viewportHeight);
            float distanceFromTop = Mathf.Clamp(_contentHeight * 0.5f - position.y, 0f, _contentHeight);
            float desiredNormalized = 1f - Mathf.Clamp01((distanceFromTop - viewportHeight * 0.35f) / scrollableHeight);

            _scrollRect.verticalNormalizedPosition = desiredNormalized;
            _focusRoutine = null;
        }

        private MapLevelNodeDefinition FindCurrentNode()
        {
            if (_mapDefinition == null || _mapDefinition.Nodes == null)
                return null;

            MapLevelNodeDefinition lastUnlocked = null;
            for (int i = 0; i < _mapDefinition.Nodes.Count; i++)
            {
                MapLevelNodeDefinition node = _mapDefinition.Nodes[i];
                if (node == null || node.Level == null)
                    continue;

                if (!ArcadeProgressService.IsNodeUnlocked(_mapDefinition, node))
                    continue;

                lastUnlocked = node;
                if (!ArcadeProgressService.IsLevelPassed(node.Level))
                    return node;
            }

            return lastUnlocked;
        }
        #endregion

        #region General Helpers
        private void ClearChildren(RectTransform root)
        {
            if (root == null) return;

            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private void ApplyTitlePolish(TMP_Text text, float outlineWidth)
        {
            if (text == null) return;

            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(Color.white, Color.white, new Color(0.72f, 0.9f, 1f), new Color(0.72f, 0.9f, 1f));

            if (text.fontMaterial != null)
            {
                text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
                text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.8f));
                text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0.35f, 1f, 0.3f));
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
            }
        }
        #endregion
    }
}
