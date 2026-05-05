using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.View;
using _Game.Scripts.Data;
using _Game.Scripts.Core;
using _Game.Scripts.Logic.Placement;
using _Game.Scripts.Logic.Resolve;
using _Game.Scripts.Logic.Tools;
using _Game.Scripts.Modes;
using _Game.Scripts.Modes.Levels;

namespace _Game.Scripts.Logic
{
    public partial class BlockController
    {
        #region Smooth Updates
        private void SmoothDragFollow()
        {
            if (!_isDragging) return;

            if (_dragFollowSmoothTime <= 0f)
            {
                transform.position = _targetDragPosition;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                _targetDragPosition,
                ref _dragVelocity,
                _dragFollowSmoothTime,
                _maxFollowSpeed
            );
        }

        private void SmoothScale()
        {
            if (_targetScale <= 0f) return;

            float current = transform.localScale.x;
            float next = Mathf.Lerp(current, _targetScale, Time.deltaTime * _scaleSmoothSpeed);
            transform.localScale = Vector3.one * next;
        }

        private void PlayPickupPunch()
        {
            if (_pickupPunchRoutine != null)
                StopCoroutine(_pickupPunchRoutine);

            _pickupPunchRoutine = StartCoroutine(PickupPunchRoutine());
        }

        private IEnumerator PickupPunchRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _pickupPunchDuration);
            float baseScale = _targetScale;
            float punchScale = baseScale * Mathf.Max(1f, _pickupPunchScale);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.one * Mathf.Lerp(baseScale, punchScale, wave);
                yield return null;
            }
        }
        #endregion

        #region Spawn Tool Attention
        private IEnumerator SpawnToolAttentionRoutine()
        {
            Vector3 baseScale = transform.localScale;

            while (true)
            {
                float wave = (Mathf.Sin(Time.time * 7.5f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(0.38f, 1f, wave);
                RestoreChildRendererAlpha(alpha);

                float scalePulse = Mathf.Lerp(0.97f, 1.03f, wave);
                transform.localScale = baseScale * scalePulse;
                yield return null;
            }
        }

        private void RestoreChildRendererAlpha(float alpha)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = renderers[i].color;
                color.a = alpha;
                renderers[i].color = color;
            }
        }
        #endregion

        #region Input Gate
        private bool IsInputAllowed()
        {
            if (GameManager.Instance == null) return true;
            return GameManager.Instance.IsInputAllowed();
        }
        #endregion
    }
}
