using System;
using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class FreePuzzleStartModeHandler : IPuzzleStartModeHandler
    {
        private readonly IPuzzleProgressRepository progressRepository;

        public FreePuzzleStartModeHandler(IPuzzleProgressRepository progressRepository)
        {
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        }

        public PuzzleStartMode Mode => PuzzleStartMode.Free;

        public Task<PuzzleStartAttempt> StartAsync(
            PuzzleDefinition puzzle,
            int pieceCount,
            StartOption startOption,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progressRepository.MarkStarted(puzzle.Id, pieceCount);
            return Task.FromResult(PuzzleStartAttempt.Success());
        }
    }

    public sealed class CoinPuzzleStartModeHandler : IPuzzleStartModeHandler
    {
        private readonly IPuzzleProgressRepository progressRepository;
        private readonly IPurchaseService purchaseService;

        public CoinPuzzleStartModeHandler(
            IPuzzleProgressRepository progressRepository,
            IPurchaseService purchaseService)
        {
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
            this.purchaseService = purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
        }

        public PuzzleStartMode Mode => PuzzleStartMode.Coins;

        public async Task<PuzzleStartAttempt> StartAsync(
            PuzzleDefinition puzzle,
            int pieceCount,
            StartOption startOption,
            CancellationToken cancellationToken)
        {
            PurchaseRequest request = new PurchaseRequest(
                BuildProductId(puzzle, pieceCount),
                CurrencyCodes.Coins,
                startOption.CoinPrice);
            PurchaseResult purchase = await purchaseService.PurchaseAsync(request, cancellationToken);
            if (!purchase.IsSuccess)
            {
                return PuzzleStartAttempt.Failure(MapPurchaseFailure(purchase.FailureReason));
            }

            cancellationToken.ThrowIfCancellationRequested();
            progressRepository.MarkStarted(puzzle.Id, pieceCount);
            return PuzzleStartAttempt.Success();
        }

        private static string BuildProductId(PuzzleDefinition puzzle, int pieceCount)
        {
            return puzzle.Id.Value + ":" + pieceCount;
        }

        private static PuzzleStartFailureReason MapPurchaseFailure(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.InsufficientFunds:
                    return PuzzleStartFailureReason.NotEnoughCoins;
                case PurchaseFailureReason.Unavailable:
                    return PuzzleStartFailureReason.PurchaseUnavailable;
                case PurchaseFailureReason.Cancelled:
                    return PuzzleStartFailureReason.PurchaseCancelled;
                default:
                    return PuzzleStartFailureReason.PurchaseUnavailable;
            }
        }
    }

    public sealed class RewardedAdPuzzleStartModeHandler : IPuzzleStartModeHandler
    {
        private readonly IPuzzleProgressRepository progressRepository;
        private readonly IAdService adService;

        public RewardedAdPuzzleStartModeHandler(
            IPuzzleProgressRepository progressRepository,
            IAdService adService)
        {
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
            this.adService = adService ?? throw new ArgumentNullException(nameof(adService));
        }

        public PuzzleStartMode Mode => PuzzleStartMode.RewardedAd;

        public async Task<PuzzleStartAttempt> StartAsync(
            PuzzleDefinition puzzle,
            int pieceCount,
            StartOption startOption,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AdAvailability availability = adService.GetRewardedAvailability(AdPlacement.PuzzleStartReward);
            if (!availability.IsAvailable)
            {
                return PuzzleStartAttempt.Failure(MapAdFailure(availability.FailureReason));
            }

            AdShowResult adResult = await adService.ShowRewardedAsync(AdPlacement.PuzzleStartReward, cancellationToken);
            if (!adResult.IsCompleted)
            {
                return PuzzleStartAttempt.Failure(MapAdFailure(adResult.FailureReason));
            }

            cancellationToken.ThrowIfCancellationRequested();
            progressRepository.MarkStarted(puzzle.Id, pieceCount);
            return PuzzleStartAttempt.Success();
        }

        private static PuzzleStartFailureReason MapAdFailure(AdShowFailureReason reason)
        {
            switch (reason)
            {
                case AdShowFailureReason.WeakInternet:
                    return PuzzleStartFailureReason.WeakInternet;
                case AdShowFailureReason.Skipped:
                    return PuzzleStartFailureReason.AdSkipped;
                default:
                    return PuzzleStartFailureReason.AdUnavailable;
            }
        }
    }
}

