using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.Scripts.View.UI; 

namespace _Game.Scripts.View.UI.Dialogs
{
    public class GuideDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _contentText;
        [SerializeField] private TextMeshProUGUI _pageNumberText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("Content Data")]
        [TextArea(3, 10)] 
        [SerializeField] private List<string> _guidePages;

        private int _currentIndex = 0;

        private void OnEnable()
        {
            if (_guidePages == null || _guidePages.Count == 0)
            {
                InitializeDefaultContent();
            }

            _currentIndex = 0;
            UpdateUI();
        }

        #region Navigation Logic (Đã sửa để Loop)

        public void OnNextClicked()
        {
            if (_guidePages == null || _guidePages.Count == 0) return;

            // Logic vòng lặp: (Trang hiện tại + 1) chia lấy dư cho Tổng số trang
            // Ví dụ: Đang ở trang cuối (index 4), tổng 5 trang -> (4 + 1) % 5 = 0 (Về đầu)
            _currentIndex = (_currentIndex + 1) % _guidePages.Count;
            
            UpdateUI();
        }

        public void OnPrevClicked()
        {
            if (_guidePages == null || _guidePages.Count == 0) return;

            // Logic vòng lặp ngược: Cộng thêm Count trước khi chia dư để tránh số âm
            // Ví dụ: Đang ở trang đầu (index 0), tổng 5 trang -> (0 - 1 + 5) % 5 = 4 (Về cuối)
            _currentIndex = (_currentIndex - 1 + _guidePages.Count) % _guidePages.Count;
            
            UpdateUI();
        }

        public void OnCloseClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnDialogCloseClicked();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Visual Update

        private void UpdateUI()
        {
            bool hasContent = _guidePages != null && _guidePages.Count > 0;

            // 1. Cập nhật nội dung Text
            if (_contentText != null)
            {
                _contentText.text = hasContent ? _guidePages[_currentIndex] : "";
            }

            // 2. Cập nhật số trang
            if (_pageNumberText != null)
            {
                _pageNumberText.text = hasContent ? $"{_currentIndex + 1} / {_guidePages.Count}" : "0 / 0";
            }

            // 3. Cập nhật trạng thái nút bấm
            // Bây giờ nút luôn sáng (interactable = true) nếu có nội dung, vì ta cho phép bấm xoay vòng
            if (_prevButton != null) _prevButton.interactable = hasContent;
            if (_nextButton != null) _nextButton.interactable = hasContent;
        }

        #endregion

        #region Default Data
        private void InitializeDefaultContent()
        {
            _guidePages = new List<string>
            {
                "<align=\"center\"><b>HOW TO PLAY</b></align>\n\n" +
                "1. Drag and drop the blocks from the bottom tray onto the 9x9 grid.\n" +
                "2. Blocks cannot be rotated (unless specified).\n" +
                "3. Plan your moves carefully!",

                "<align=\"center\"><b>CLEARING LINES</b></align>\n\n" +
                "Fill any row or column completely to clear it.\n" +
                "Cleared lines disappear and grant you points.\n" +
                "Try to clear multiple lines at once for a higher score!",

                "<align=\"center\"><b>SCORING RULES</b></align>\n\n" +
                "• <b>Base Score:</b> 100 points per line cleared.\n" +
                "• <b>Multi-Line Bonus:</b> Clearing 2+ lines at once multiplies your score.\n" +
                "• <b>Block Placement:</b> You also get small points just for placing a block.",

                "<align=\"center\"><b>COMBO STREAK</b></align>\n\n" +
                "Clear lines in consecutive moves to trigger a <b>COMBO</b>.\n" +
                "The higher your Combo Streak, the more bonus points you earn.\n" +
                "Keep the rhythm going!",

                "<align=\"center\"><b>GAME OVER</b></align>\n\n" +
                "The game ends when there is no space left on the grid for any of the pending blocks.\n\n" +
                "<i>Good luck and have fun!</i>"
            };
        }
        #endregion
    }
}