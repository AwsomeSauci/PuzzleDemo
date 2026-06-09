using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Presentation.Gallery
{
    public sealed class PuzzleGalleryItemViewModel
    {
        public PuzzleGalleryItemViewModel(PuzzleDefinition puzzle, Sprite preview)
        {
            Puzzle = puzzle;
            Preview = preview;
        }

        public PuzzleDefinition Puzzle { get; }
        public Sprite Preview { get; }
    }
}

