using System.Collections;
using TMPro;
using UnityEngine;
using _Game.Scripts.Core;
using _Game.Scripts.View.UI; // Cần dòng này để gọi UIManager (nếu UIManager nằm trong namespace này)

namespace _Game.Scripts.View.UI.Dialogs
{
    public class MusicChangeDialog : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text hiển thị tên bài hát (Yêu cầu Pivot 0.5, 0.5)")]
        [SerializeField] private TextMeshProUGUI _songLabel;
        
        [Tooltip("Object cha chứa Label (Bắt buộc phải có component RectMask2D để che phần chữ thừa)")]
        [SerializeField] private RectTransform _labelContainer; 

        [Header("Animation Config")]
        [SerializeField] private float _slideDuration = 0.2f;
        [SerializeField] private float _scrollSpeed = 50f;
        [SerializeField] private float _scrollDelay = 1.5f; // Delay lâu hơn chút để người dùng kịp đọc tiêu đề

        private int _previewIndex;
        private int _originalIndex;
        private Coroutine _scrollCoroutine;
        private Coroutine _slideCoroutine;

        private void OnEnable()
        {
            if (AudioManager.Instance == null) return;

            // 1. Lưu lại index gốc để hoàn tác nếu đóng mà không lưu
            _originalIndex = AudioManager.Instance.GetCurrentBGMIndex();
            _previewIndex = _originalIndex;

            // 2. [QUAN TRỌNG] Ép buộc Text về căn giữa chuẩn xác
            if (_songLabel != null)
            {
                _songLabel.alignment = TextAlignmentOptions.Center;
                _songLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _songLabel.rectTransform.anchoredPosition = Vector2.zero;
            }

            // 3. Cập nhật giao diện ngay
            UpdateUI(false); 
        }

        private void OnDisable()
        {
            // Dừng mọi animation khi đóng dialog
            StopAllCoroutines();
        }

        #region Button Events

        public void OnNextClicked()
        {
            ChangeSong(1);
        }

        public void OnPrevClicked()
        {
            ChangeSong(-1);
        }

        public void OnApplyClicked()
        {
            if (AudioManager.Instance != null)
            {
                // Lưu cấu hình
                AudioManager.Instance.SaveBGMIndex(_previewIndex);
                AudioManager.Instance.PlayClickSound();
                Debug.Log("Music Applied Index: " + _previewIndex);
                
                // Cập nhật lại original để nếu bấm đóng sau khi apply thì không bị revert nhạc
                _originalIndex = _previewIndex; 
            }
        }

        public void OnCloseClicked()
        {
            // Nếu thoát mà chưa Apply -> Hoàn tác về bài nhạc cũ
            if (_previewIndex != _originalIndex && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(_originalIndex);
            }
            
            // Gọi UIManager để đóng đúng quy trình (Resume game...)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnDialogCloseClicked();
            }
        }

        #endregion

        #region Logic & Visual Effects

        private void ChangeSong(int direction)
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.PlayClickSound();

            int count = AudioManager.Instance.GetBGMCount();
            if (count == 0) return;

            // Tính index tiếp theo (Vòng tròn)
            _previewIndex += direction;
            if (_previewIndex >= count) _previewIndex = 0;
            if (_previewIndex < 0) _previewIndex = count - 1;

            // Nghe thử ngay lập tức (Preview)
            AudioManager.Instance.PlayBGM(_previewIndex);

            // Chạy hiệu ứng trượt chuyển bài (Slide)
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideRoutine(direction));
        }

        private void UpdateUI(bool animate)
        {
            if (AudioManager.Instance == null) return;
            AudioClip clip = AudioManager.Instance.GetBGMClip(_previewIndex);
            string songName = clip != null ? clip.name : "Unknown";

            _songLabel.text = songName;
            
            // [FIX] Luôn reset về vị trí giữa (0,0) mỗi khi đổi tên để tránh bị lệch
            _songLabel.rectTransform.anchoredPosition = Vector2.zero;

            // Bắt đầu chạy chữ (Marquee) nếu tên quá dài
            if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = StartCoroutine(MarqueeRoutine());
        }

        // Hiệu ứng trượt chữ: Chữ cũ bay ra, chữ mới bay vào
        private IEnumerator SlideRoutine(int direction)
        {
            float width = _labelContainer.rect.width;
            
            // Với Pivot Center (0.5, 0.5), tọa độ 0 là chính giữa
            Vector2 centerPos = Vector2.zero;
            Vector2 outPos = new Vector2(-direction * width, 0); // Bay ra hướng ngược chiều bấm
            Vector2 inPos = new Vector2(direction * width, 0);   // Bay vào từ hướng bấm

            RectTransform rt = _songLabel.rectTransform;
            float t = 0;

            // 1. Slide Out (Chữ cũ bay đi)
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / (_slideDuration / 2);
                rt.anchoredPosition = Vector2.Lerp(centerPos, outPos, t);
                yield return null;
            }

            // 2. Đổi nội dung text
            UpdateUI(false); 
            // Lưu ý: UpdateUI vừa reset vị trí về 0, ta cần set lại nó về vị trí "chuẩn bị bay vào"
            rt.anchoredPosition = inPos;

            // 3. Slide In (Chữ mới bay vào)
            t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / (_slideDuration / 2);
                rt.anchoredPosition = Vector2.Lerp(inPos, centerPos, t);
                yield return null;
            }
            rt.anchoredPosition = centerPos;
        }

        // Hiệu ứng chữ chạy (Marquee) - Đã chỉnh sửa toán học cho Pivot Center
        private IEnumerator MarqueeRoutine()
        {
            // Đợi 1 frame để TextMeshPro tính toán độ rộng chính xác sau khi set text
            yield return null; 

            float textWidth = _songLabel.preferredWidth;
            float containerWidth = _labelContainer.rect.width;

            // Chỉ chạy nếu chữ dài hơn khung chứa
            if (textWidth > containerWidth)
            {
                RectTransform rt = _songLabel.rectTransform;
                
                // Đợi một chút để người chơi đọc tiêu đề tĩnh trước khi chạy
                yield return new WaitForSecondsRealtime(_scrollDelay);

                while (true)
                {
                    // Di chuyển từ phải sang trái
                    Vector2 pos = rt.anchoredPosition;
                    pos.x -= _scrollSpeed * Time.unscaledDeltaTime;
                    rt.anchoredPosition = pos;

                    // Logic kiểm tra biên giới hạn (Boundary Check) với Pivot Center:
                    // Khi cạnh PHẢI của text (pos.x + textWidth/2) trôi qua cạnh TRÁI của container (-containerWidth/2)
                    if (pos.x + textWidth / 2 < -containerWidth / 2) 
                    {
                        // Reset về bên phải: Đặt cạnh TRÁI của text tại cạnh PHẢI container
                        // Cần tính tọa độ tâm (pos.x) sao cho: pos.x - textWidth/2 = containerWidth/2
                        float resetX = (containerWidth / 2) + (textWidth / 2);
                        rt.anchoredPosition = new Vector2(resetX, 0);
                    }

                    yield return null;
                }
            }
            else
            {
                // Nếu text ngắn, đảm bảo nó nằm im chính giữa
                _songLabel.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        #endregion
    }
}