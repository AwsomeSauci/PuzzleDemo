using System.Collections.Generic;
using System;

namespace PuzzleFlow.Presentation.Gallery
{
    public interface IPuzzleGalleryView
    {
        event Action<int> ItemBecameVisible;

        void Render(IReadOnlyList<PuzzleGalleryItemViewModel> items);
        void UpdateItem(int itemIndex, PuzzleGalleryItemViewModel item);
    }
}
