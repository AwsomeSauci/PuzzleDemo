using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleStartStateService
    {
        int SelectInitialPieceCount(PuzzleId puzzleId);
        PuzzleStartState GetState(PuzzleId puzzleId, int pieceCount);
    }
}

