using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleMediaQueryService
    {
        MediaReference GetPreviewMedia(PuzzleId puzzleId);
    }
}

