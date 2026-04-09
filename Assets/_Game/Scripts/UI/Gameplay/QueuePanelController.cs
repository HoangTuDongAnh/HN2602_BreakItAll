using BreakItAll.Gameplay;
using System.Collections.Generic;
using UnityEngine;

namespace BreakItAll.UI
{
    public sealed class QueuePanelController : MonoBehaviour
    {
        [SerializeField] private QueueSlotView[] slotViews;

        public void Refresh(IReadOnlyList<ShapeData> queue)
        {
            if (slotViews == null || slotViews.Length == 0)
            {
                return;
            }

            for (int i = 0; i < slotViews.Length; i++)
            {
                QueueSlotView slot = slotViews[i];
                if (slot == null)
                {
                    continue;
                }

                if (queue != null && i < queue.Count && queue[i] != null)
                {
                    slot.SetData(i, queue[i].Id, queue[i].Cells.Count);
                }
                else
                {
                    slot.SetEmpty(i);
                }
            }
        }
    }
}