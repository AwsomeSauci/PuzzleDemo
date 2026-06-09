using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleProgressRepository
    {
        PuzzleProgress GetProgress(PuzzleId puzzleId, int pieceCount);
        void MarkStarted(PuzzleId puzzleId, int pieceCount);
    }
}

