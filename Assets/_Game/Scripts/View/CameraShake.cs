using UnityEngine;
using _Game.Scripts.Core;

namespace _Game.Scripts.View
{
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _originalPos;
        private float _shakeDuration = 0f;
        private float _shakeMagnitude = 0.1f;
        private float _dampingSpeed = 2.0f;

        private void Start()
        {
            _originalPos = transform.localPosition;
        }

        private void OnEnable() => GameEvents.OnComboUpdated += TriggerShake;
        private void OnDisable() => GameEvents.OnComboUpdated -= TriggerShake;

        private void TriggerShake(int comboStreak)
        {
            if (comboStreak <= 0) return;

            // Combo càng cao rung càng mạnh
            _shakeMagnitude = 0.1f + (comboStreak * 0.05f); 
            _shakeDuration = 0.2f + (comboStreak * 0.05f);
            
            // Giới hạn để không rung quá chóng mặt
            _shakeMagnitude = Mathf.Clamp(_shakeMagnitude, 0.1f, 0.5f);
            _shakeDuration = Mathf.Clamp(_shakeDuration, 0.2f, 0.6f);
        }

        private void Update()
        {
            if (_shakeDuration > 0)
            {
                transform.localPosition = _originalPos + Random.insideUnitSphere * _shakeMagnitude;
                _shakeDuration -= Time.deltaTime * _dampingSpeed;
            }
            else
            {
                _shakeDuration = 0f;
                transform.localPosition = _originalPos;
            }
        }
    }
}