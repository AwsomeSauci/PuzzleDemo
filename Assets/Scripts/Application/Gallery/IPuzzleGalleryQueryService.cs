using System.Collections.Generic;

namespace PuzzleFlow.Application
{
    public interface IPuzzleGalleryQueryService
    {
        IReadOnlyList<PuzzleGalleryItemData> GetItems();
    }
}

