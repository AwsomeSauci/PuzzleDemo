using System;
using System.Collections.Generic;
using PuzzleFlow.Application;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Infrastructure
{
    public sealed class InMemoryPuzzleProgressRepository : IPuzzleProgressRepository
    {
        private readonly Dictionary<string, PuzzleProgress> progressByKey = new Dictionary<string, PuzzleProgress>();

        public InMemoryPuzzleProgressRepository()
            : this(Array.Empty<SerializedPuzzleProgressSeed>())
        {
        }

        public InMemoryPuzzleProgressRepository(IReadOnlyList<SerializedPuzzleProgressSeed> seeds)
        {
            if (seeds == null)
            {
                throw new ArgumentNullException(nameof(seeds));
            }

            for (int index = 0; index < seeds.Count; index++)
            {
                PuzzleProgress progress = seeds[index].ToProgress();
                progressByKey[BuildKey(progress.PuzzleId, progress.PieceCount)] = progress;
            }
        }

        public PuzzleProgress GetProgress(PuzzleId puzzleId, int pieceCount)
        {
            progressByKey.TryGetValue(BuildKey(puzzleId, pieceCount), out PuzzleProgress progress);
            return progress;
        }

        public void MarkStarted(PuzzleId puzzleId, int pieceCount)
        {
            progressByKey[BuildKey(puzzleId, pieceCount)] = new PuzzleProgress(puzzleId, pieceCount, 1);
        }

        private static string BuildKey(PuzzleId puzzleId, int pieceCount)
        {
            return puzzleId.Value + ":" + pieceCount;
        }
    }
}
