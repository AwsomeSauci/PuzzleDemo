using UnityEngine;

namespace PuzzleFlow.Presentation.Virtualization
{
    public interface IVirtualizedGridCell<in TItem>
    {
        RectTransform RectTransform { get; }
        void Bind(TItem item, int itemIndex);
        void Unbind();
    }
}
