using _Game.Scripts.Core.Arcade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.View.UI.Components
{
    /// <summary>
    /// Reusable coin display for Home, Arcade Map and result dialogs.
    /// Place this on a small top-bar widget and assign Coin Text/Icon in the prefab.
    /// It reads coin data from ArcadeProgressService and refreshes whenever coins change.
    /// </summary>
    public class CoinDisplayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private Image _coinIcon;

        [Header("Format")]
        [SerializeField] private string _prefix;
        [SerializeField] private string _suffix;
        [SerializeField] private bool _useCompactFormat = true;
        [SerializeField] private bool _refreshOnEnable = true;

        private int _lastValue = int.MinValue;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            ArcadeProgressService.OnCoinsChanged += HandleCoinsChanged;

            if (_refreshOnEnable)
                Refresh();
        }

        private void OnDisable()
        {
            ArcadeProgressService.OnCoinsChanged -= HandleCoinsChanged;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureReferences();
                UnityEditor.EditorUtility.SetDirty(this);
            };
        }
#endif

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            SetCoins(ArcadeProgressService.GetCoins());
        }

        private void HandleCoinsChanged(int coins)
        {
            SetCoins(coins);
        }

        private void SetCoins(int coins)
        {
            if (_lastValue == coins && _coinText != null && !string.IsNullOrEmpty(_coinText.text))
                return;

            _lastValue = coins;

            if (_coinText != null)
                _coinText.text = $"{_prefix}{FormatCoins(coins)}{_suffix}";
        }

        private string FormatCoins(int coins)
        {
            if (!_useCompactFormat)
                return coins.ToString();

            if (coins >= 1000000)
                return $"{coins / 1000000f:0.#}M";

            if (coins >= 1000)
                return $"{coins / 1000f:0.#}K";

            return coins.ToString();
        }

        [ContextMenu("Auto Setup References")]
        private void EnsureReferences()
        {
            if (_coinText == null)
                _coinText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_coinIcon == null)
                _coinIcon = GetComponentInChildren<Image>(true);
        }
    }
}
