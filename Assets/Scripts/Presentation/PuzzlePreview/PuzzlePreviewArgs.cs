using PuzzleFlow.Domain;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzlePreviewArgs
    {
        public PuzzlePreviewArgs(PuzzleId puzzleId)
        {
            PuzzleId = puzzleId;
        }

        public PuzzleId PuzzleId { get; }
    }
}

