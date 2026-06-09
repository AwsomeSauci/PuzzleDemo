using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleGallerySortConfig
    {
        int GetPriority(PuzzleStartMode mode);
    }
}

