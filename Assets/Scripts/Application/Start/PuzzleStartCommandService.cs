using System;
using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleStartCommandService : IPuzzleStartCommandService
    {
        private readonly IPuzzleCatalogQueryService catalogQueryService;
        private readonly IPuzzleStartService startService;

        public PuzzleStartCommandService(
            IPuzzleCatalogQueryService catalogQueryService,
            IPuzzleStartService startService)
        {
            this.catalogQueryService = catalogQueryService ?? throw new ArgumentNullException(nameof(catalogQueryService));
            this.startService = startService ?? throw new ArgumentNullException(nameof(startService));
        }

        public Task<PuzzleStartAttempt> StartNewAsync(
            PuzzleId puzzleId,
            int pieceCount,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PuzzleDefinition puzzle = catalogQueryService.GetById(puzzleId);
            return startService.StartNewAsync(puzzle, pieceCount, cancellationToken);
        }
    }
}
