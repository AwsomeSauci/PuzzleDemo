using System;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleContinueService : IPuzzleContinueService
    {
        private readonly IPuzzleProgressRepository progressRepository;

        public PuzzleContinueService(IPuzzleProgressRepository progressRepository)
        {
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        }

        public bool CanContinue(PuzzleId puzzleId, int pieceCount)
        {
            PuzzleProgress progress = progressRepository.GetProgress(puzzleId, pieceCount);
            return progress != null && progress.CanContinue;
        }
    }
}

