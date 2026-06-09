using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleContinueService
    {
        bool CanContinue(PuzzleId puzzleId, int pieceCount);
    }
}

