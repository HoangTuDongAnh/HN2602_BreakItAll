using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BreakItAll.UI
{
    public sealed class GameplayHUDController : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text statusText;

        [Header("Placement Input")]
        [SerializeField] private TMP_InputField queueIndexInput;
        [SerializeField] private TMP_InputField anchorXInput;
        [SerializeField] private TMP_InputField anchorYInput;
        [SerializeField] private Button placeButton;

        private Action<int, int, int> _onPlaceRequested;

        private void Awake()
        {
            if (placeButton != null)
            {
                placeButton.onClick.AddListener(HandlePlaceClicked);
            }
        }

        private void OnDestroy()
        {
            if (placeButton != null)
            {
                placeButton.onClick.RemoveListener(HandlePlaceClicked);
            }
        }

        public void BindPlaceRequest(Action<int, int, int> onPlaceRequested)
        {
            _onPlaceRequested = onPlaceRequested;
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void SetCombo(int combo)
        {
            if (comboText != null)
            {
                comboText.text = $"Combo: {combo}";
            }
        }

        public void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        private void HandlePlaceClicked()
        {
            int queueIndex = ParseOrDefault(queueIndexInput, 0);
            int anchorX = ParseOrDefault(anchorXInput, 0);
            int anchorY = ParseOrDefault(anchorYInput, 0);

            _onPlaceRequested?.Invoke(queueIndex, anchorX, anchorY);
        }

        private int ParseOrDefault(TMP_InputField inputField, int defaultValue)
        {
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
            {
                return defaultValue;
            }

            return int.TryParse(inputField.text, out int value) ? value : defaultValue;
        }
    }
}