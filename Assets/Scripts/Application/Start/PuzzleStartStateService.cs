using PuzzleFlow.Domain;
using System;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleStartStateService : IPuzzleStartStateService
    {
        private readonly IPuzzleCatalogQueryService catalogQueryService;
        private readonly IPuzzleProgressRepository progressRepository;
        private readonly IPurchaseService purchaseService;
        private readonly IStartOptionResolver startOptionResolver;

        public PuzzleStartStateService(
            IPuzzleCatalogQueryService catalogQueryService,
            IPuzzleProgressRepository progressRepository,
            IPurchaseService purchaseService,
            IStartOptionResolver startOptionResolver)
        {
            this.catalogQueryService = catalogQueryService ?? throw new ArgumentNullException(nameof(catalogQueryService));
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
            this.purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
            this.startOptionResolver = startOptionResolver ?? throw new ArgumentNullException(nameof(startOptionResolver));
        }

        public int SelectInitialPieceCount(PuzzleId puzzleId)
        {
            PuzzleDefinition puzzle = catalogQueryService.GetById(puzzleId);
            if (puzzle.CutOptions == null || puzzle.CutOptions.Count == 0)
            {
                throw new InvalidOperationException($"Puzzle '{puzzleId}' has no cut options.");
            }

            for (int index = 0; index < puzzle.CutOptions.Count; index++)
            {
                int pieceCount = puzzle.CutOptions[index].PieceCount;
                PuzzleProgress progress = progressRepository.GetProgress(puzzle.Id, pieceCount);
                if (progress != null && progress.CanContinue)
                {
                    return pieceCount;
                }
            }

            return puzzle.CutOptions[0].PieceCount;
        }

        public PuzzleStartState GetState(PuzzleId puzzleId, int pieceCount)
        {
            PuzzleDefinition puzzle = catalogQueryService.GetById(puzzleId);
            return new PuzzleStartState(
                puzzle,
                pieceCount,
                startOptionResolver.Resolve(puzzle, pieceCount),
                progressRepository.GetProgress(puzzleId, pieceCount),
                purchaseService.GetBalance(CurrencyCodes.Coins));
        }
    }
}

