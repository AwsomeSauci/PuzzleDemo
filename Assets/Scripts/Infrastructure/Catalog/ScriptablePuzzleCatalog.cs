using System;
using System.Collections.Generic;
using PuzzleFlow.Application;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Infrastructure
{
    public sealed class ScriptablePuzzleCatalog : IPuzzleCatalog, IPuzzleMediaCatalog
    {
        private readonly List<PuzzleDefinition> puzzles = new List<PuzzleDefinition>();
        private readonly Dictionary<PuzzleId, PuzzleDefinition> puzzlesById = new Dictionary<PuzzleId, PuzzleDefinition>();
        private readonly Dictionary<PuzzleId, MediaReference> previewsByPuzzleId = new Dictionary<PuzzleId, MediaReference>();

        public ScriptablePuzzleCatalog(PuzzleCatalogAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            IReadOnlyList<PuzzleCatalogEntry> entries = asset.Entries;
            if (entries.Count == 0)
            {
                throw new InvalidOperationException("Puzzle catalog asset is empty.");
            }

            for (int index = 0; index < entries.Count; index++)
            {
                PuzzleCatalogEntry entry = entries[index];
                if (entry == null)
                {
                    throw new InvalidOperationException($"Puzzle catalog entry at index {index} is null.");
                }

                PuzzleDefinition puzzle = entry.ToDefinition();
                if (puzzlesById.ContainsKey(puzzle.Id))
                {
                    throw new InvalidOperationException($"Duplicate puzzle id '{puzzle.Id}'.");
                }

                puzzles.Add(puzzle);
                puzzlesById[puzzle.Id] = puzzle;
                if (entry.PreviewMedia.IsEmpty)
                {
                    throw new InvalidOperationException($"Puzzle '{puzzle.Id}' has empty preview media reference.");
                }

                previewsByPuzzleId[puzzle.Id] = entry.PreviewMedia;
            }
        }

        public IReadOnlyList<PuzzleDefinition> GetAll()
        {
            return puzzles;
        }

        public PuzzleDefinition Get(PuzzleId puzzleId)
        {
            if (puzzlesById.TryGetValue(puzzleId, out PuzzleDefinition puzzle))
            {
                return puzzle;
            }

            throw new KeyNotFoundException($"Puzzle '{puzzleId}' was not found.");
        }

        public MediaReference GetPreviewMedia(PuzzleId puzzleId)
        {
            if (previewsByPuzzleId.TryGetValue(puzzleId, out MediaReference reference) && !reference.IsEmpty)
            {
                return reference;
            }

            throw new KeyNotFoundException($"Puzzle '{puzzleId}' has no preview media.");
        }
    }
}
