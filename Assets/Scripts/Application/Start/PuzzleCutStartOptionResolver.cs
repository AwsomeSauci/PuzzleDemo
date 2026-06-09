using System;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleCutStartOptionResolver : IStartOptionResolver
    {
        public StartOption Resolve(PuzzleDefinition puzzle, int pieceCount)
        {
            if (puzzle.TryGetCut(pieceCount, out PuzzleCutDefinition cut))
            {
                return cut.StartOption;
            }

            throw new InvalidOperationException(
                $"Puzzle '{puzzle.Id}' has no start option for {pieceCount} pieces.");
        }
    }
}

