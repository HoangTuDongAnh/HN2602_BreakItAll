using _Game.Scripts.Core;
using _Game.Scripts.Core.Arcade;
using _Game.Scripts.Logic;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;
using _Game.Scripts.Modes.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI
{
    public partial class GameplayHUD
    {
        #region Reference Helpers
        private void EnsureReferences()
        {
            if (_endlessRoot == null)
                _endlessRoot = FindChildGameObject("Endless_Root");

            if (_arcadeRoot == null)
                _arcadeRoot = FindChildGameObject("Arcade_Root");

            Transform arcadeRoot = _arcadeRoot != null ? _arcadeRoot.transform : transform;

            if (_levelLabelRoot == null)
                _levelLabelRoot = FindChildGameObject("Label_Level");

            if (_levelText == null)
                _levelText = FindText("Text_Level");

            _scoreLabelRoot = EnsureLabelRoot(_scoreLabelRoot, arcadeRoot, "Label_Score", "Label_Time", new Vector2(0f, -128f), new Vector2(360f, 78f));
            _collectableLabelRoot = EnsureLabelRoot(_collectableLabelRoot, arcadeRoot, "Label_Collectable", null, new Vector2(0f, -128f), new Vector2(360f, 78f));
            _shapeLabelRoot = EnsureLabelRoot(_shapeLabelRoot, arcadeRoot, "Label_Shape", null, new Vector2(0f, -128f), new Vector2(360f, 72f));
            _puzzleLabelRoot = EnsureLabelRoot(_puzzleLabelRoot, arcadeRoot, "Label_Puzzle", "Label_Fill", new Vector2(0f, -128f), new Vector2(430f, 118f));

            if (_scoreTimerText == null)
                _scoreTimerText = FindTextInRoot(_scoreLabelRoot, "Text_ScoreTime") ?? FindText("Text_Time");
            if (_scoreTimerText == null)
                _scoreTimerText = EnsureText(_scoreLabelRoot.transform, "Text_ScoreTime", new Vector2(0.5f, 0.5f), new Vector2(-92f, 0f), 26f);

            if (_scoreProgressText == null)
                _scoreProgressText = FindTextInRoot(_scoreLabelRoot, "Text_ScoreProgress") ?? FindText("Text_ArcadeProgress");
            if (_scoreProgressText == null)
                _scoreProgressText = EnsureText(_scoreLabelRoot.transform, "Text_ScoreProgress", new Vector2(0.5f, 0.5f), new Vector2(92f, 0f), 24f);

            if (_collectableTimerText == null)
                _collectableTimerText = FindTextInRoot(_collectableLabelRoot, "Text_CollectableTime");
            if (_collectableTimerText == null)
                _collectableTimerText = EnsureText(_collectableLabelRoot.transform, "Text_CollectableTime", new Vector2(0.5f, 0.5f), new Vector2(-92f, 0f), 26f);

            if (_collectableProgressText == null)
                _collectableProgressText = FindTextInRoot(_collectableLabelRoot, "Text_CollectableProgress");
            if (_collectableProgressText == null)
                _collectableProgressText = EnsureText(_collectableLabelRoot.transform, "Text_CollectableProgress", new Vector2(0.5f, 0.5f), new Vector2(92f, 0f), 24f);

            if (_shapeOverlayButton == null)
                _shapeOverlayButton = FindButtonInRoot(_shapeLabelRoot, "Button_ShapeOverlay");
            if (_shapeOverlayButton == null)
                _shapeOverlayButton = EnsureButton(_shapeLabelRoot.transform, "Button_ShapeOverlay", "Overlay Off", new Vector2(0.5f, 0.5f), new Vector2(-88f, 0f), new Vector2(128f, 46f));

            if (_shapeOverlayButtonText == null && _shapeOverlayButton != null)
                _shapeOverlayButtonText = _shapeOverlayButton.GetComponentInChildren<TextMeshProUGUI>();

            if (_shapeProgressText == null)
                _shapeProgressText = FindTextInRoot(_shapeLabelRoot, "Text_ShapeProgress");
            if (_shapeProgressText == null)
                _shapeProgressText = EnsureText(_shapeLabelRoot.transform, "Text_ShapeProgress", new Vector2(0.5f, 0.5f), new Vector2(94f, 0f), 22f);

            if (_puzzleTimerText == null)
                _puzzleTimerText = FindTextInRoot(_puzzleLabelRoot, "Text_PuzzleTime");
            if (_puzzleTimerText == null)
                _puzzleTimerText = EnsureText(_puzzleLabelRoot.transform, "Text_PuzzleTime", new Vector2(0.5f, 0.5f), new Vector2(0f, 32f), 24f);

            if (_puzzleSwitchButton == null)
                _puzzleSwitchButton = FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzleSwitch")
                                      ?? FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzleNext")
                                      ?? FindButtonInRoot(_puzzleLabelRoot, "Button_FillNext");
            if (_puzzleSwitchButton == null)
                _puzzleSwitchButton = EnsureButton(_puzzleLabelRoot.transform, "Button_PuzzleSwitch", "Switch", new Vector2(0.5f, 0.5f), new Vector2(-96f, -12f), new Vector2(132f, 46f));

            if (_puzzleSwitchButtonText == null && _puzzleSwitchButton != null)
                _puzzleSwitchButtonText = _puzzleSwitchButton.GetComponentInChildren<TextMeshProUGUI>();

            if (_puzzleRemainingText == null)
                _puzzleRemainingText = FindTextInRoot(_puzzleLabelRoot, "Text_PuzzleRemaining")
                                       ?? FindTextInRoot(_puzzleLabelRoot, "Text_PuzzlePage")
                                       ?? FindTextInRoot(_puzzleLabelRoot, "Text_FillPage");
            if (_puzzleRemainingText == null)
                _puzzleRemainingText = EnsureText(_puzzleLabelRoot.transform, "Text_PuzzleRemaining", new Vector2(0.5f, 0.5f), new Vector2(96f, -12f), 19f);

            if (_puzzlePreviousButton == null)
                _puzzlePreviousButton = FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzlePrev") ?? FindButtonInRoot(_puzzleLabelRoot, "Button_FillPrev");

            if (_puzzleNextButton == null)
                _puzzleNextButton = FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzleNext") ?? FindButtonInRoot(_puzzleLabelRoot, "Button_FillNext");

            if (_puzzlePageText == null)
                _puzzlePageText = FindTextInRoot(_puzzleLabelRoot, "Text_PuzzlePage") ?? FindTextInRoot(_puzzleLabelRoot, "Text_FillPage");

            if (_puzzleProgressText == null)
                _puzzleProgressText = FindTextInRoot(_puzzleLabelRoot, "Text_PuzzleProgress");
            if (_puzzleProgressText != null)
                _puzzleProgressText.gameObject.SetActive(false);

            if (_legacyArcadeLabelText == null)
                _legacyArcadeLabelText = FindText("Text_ArcadeType");
            if (_legacyArcadeLabelText != null)
                _legacyArcadeLabelText.gameObject.SetActive(false);

            ConfigurePuzzleQueueReferences();
        }

        private void ConfigurePuzzleQueueReferences()
        {
            if (_puzzleLabelRoot == null) return;

            if (_puzzleSwitchButton == null)
                _puzzleSwitchButton = FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzleSwitch")
                                      ?? FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzleNext")
                                      ?? FindButtonInRoot(_puzzleLabelRoot, "Button_FillNext");

            if (_puzzleSwitchButton == null)
                _puzzleSwitchButton = EnsureButton(_puzzleLabelRoot.transform, "Button_PuzzleSwitch", "Switch", new Vector2(0.5f, 0.5f), new Vector2(-96f, -12f), new Vector2(132f, 46f));

            if (_puzzleSwitchButton != null)
            {
                if (_puzzleSwitchButton.name != "Button_PuzzleSwitch")
                    _puzzleSwitchButton.name = "Button_PuzzleSwitch";

                _puzzleSwitchButtonText = _puzzleSwitchButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (_puzzleSwitchButtonText != null)
                    _puzzleSwitchButtonText.text = "Switch";
            }

            TextMeshProUGUI remainingText = FindTextInRoot(_puzzleLabelRoot, "Text_PuzzleRemaining")
                                            ?? FindTextInRoot(_puzzleLabelRoot, "Text_PuzzlePage")
                                            ?? FindTextInRoot(_puzzleLabelRoot, "Text_FillPage");

            if (remainingText == null)
                remainingText = EnsureText(_puzzleLabelRoot.transform, "Text_PuzzleRemaining", new Vector2(0.5f, 0.5f), new Vector2(96f, -12f), 19f);

            _puzzleRemainingText = remainingText;
            if (_puzzleRemainingText != null)
            {
                if (_puzzleRemainingText.name != "Text_PuzzleRemaining")
                    _puzzleRemainingText.name = "Text_PuzzleRemaining";

                if (string.IsNullOrWhiteSpace(_puzzleRemainingText.text) || _puzzleRemainingText.text.Contains("Page"))
                    _puzzleRemainingText.text = "Block Remaining: 0";
            }

            _puzzlePreviousButton = FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzlePrev")
                                    ?? FindButtonInRoot(_puzzleLabelRoot, "Button_FillPrev");

            _puzzleNextButton = FindButtonInRoot(_puzzleLabelRoot, "Button_PuzzleNext")
                                ?? FindButtonInRoot(_puzzleLabelRoot, "Button_FillNext");

            _puzzlePageText = FindTextInRoot(_puzzleLabelRoot, "Text_PuzzlePage")
                              ?? FindTextInRoot(_puzzleLabelRoot, "Text_FillPage");

            SetPuzzleNavigationVisible(IsPuzzleArcadeLevel());
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.gameObject : null;
        }

        private TextMeshProUGUI FindText(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static TextMeshProUGUI FindTextInRoot(GameObject root, string childName)
        {
            if (root == null) return null;
            Transform child = FindChild(root.transform, childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Button FindButtonInRoot(GameObject root, string childName)
        {
            if (root == null) return null;
            Transform child = FindChild(root.transform, childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;
            if (root.name == childName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static GameObject EnsureLabelRoot(GameObject current, Transform parent, string primaryName, string fallbackName, Vector2 position, Vector2 size)
        {
            if (current != null) return current;

            Transform existing = FindChild(parent, primaryName);
            if (existing == null && !string.IsNullOrEmpty(fallbackName))
                existing = FindChild(parent, fallbackName);

            if (existing != null)
                return existing.gameObject;

            GameObject obj = new GameObject(primaryName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return obj;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, Vector2 anchor, Vector2 position, float fontSize)
        {
            Transform existing = FindChild(parent, name);
            TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (text != null) return text;

            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 36f);

            text = obj.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            return text;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Transform existing = FindChild(parent, name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button != null) return button;

            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.08f, 0.45f, 0.9f, 0.9f);

            button = obj.GetComponent<Button>();

            TextMeshProUGUI text = EnsureText(obj.transform, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, Mathf.Min(20f, size.y * 0.45f));
            text.text = label;
            text.raycastTarget = false;

            RectTransform textRect = text.transform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
        #endregion
    }
}
