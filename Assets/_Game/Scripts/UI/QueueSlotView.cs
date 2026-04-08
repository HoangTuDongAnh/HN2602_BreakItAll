using TMPro;
using UnityEngine;

namespace BreakItAll.UI
{
    public sealed class QueueSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text cellsText;

        public void SetEmpty(int index)
        {
            if (titleText != null)
            {
                titleText.text = $"Slot {index}: Empty";
            }

            if (cellsText != null)
            {
                cellsText.text = "-";
            }
        }

        public void SetData(int index, string shapeId, int cellCount)
        {
            if (titleText != null)
            {
                titleText.text = $"Slot {index}: {shapeId}";
            }

            if (cellsText != null)
            {
                cellsText.text = $"Cells: {cellCount}";
            }
        }
    }
}