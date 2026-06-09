using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleMediaCatalog
    {
        MediaReference GetPreviewMedia(PuzzleId puzzleId);
    }
}

