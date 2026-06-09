using PuzzleFlow.Domain;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public enum PuzzlePreviewAction
    {
        Cancelled,
        Started,
        Continued
    }

    public sealed class PuzzlePreviewResult
    {
        public PuzzlePreviewResult(PuzzlePreviewAction action, PuzzleId puzzleId, int pieceCount)
        {
            Action = action;
            PuzzleId = puzzleId;
            PieceCount = pieceCount;
        }

        public PuzzlePreviewAction Action { get; }
        public PuzzleId PuzzleId { get; }
        public int PieceCount { get; }
    }
}

