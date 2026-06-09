using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleGalleryItemData
    {
        public PuzzleGalleryItemData(PuzzleDefinition puzzle, StartOption primaryStartOption)
        {
            Puzzle = puzzle;
            PrimaryStartOption = primaryStartOption;
        }

        public PuzzleDefinition Puzzle { get; }
        public StartOption PrimaryStartOption { get; }
    }
}

