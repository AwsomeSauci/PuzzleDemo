using NUnit.Framework;
using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PuzzleFlow.Tests
{
    public sealed class PuzzleStartServiceTests
    {
        [Test]
        public async Task FreeStart_MarksProgressAndSucceeds()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.Free, 0);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new FreePuzzleStartModeHandler(progressRepository));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Not.Null);
        }

        [Test]
        public async Task CoinStart_WhenPurchaseSucceeds_MarksProgress()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.Coins, 100);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new CoinPuzzleStartModeHandler(
                    progressRepository,
                    new FakePurchaseService(PurchaseResult.Success())));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Not.Null);
        }

        [Test]
        public async Task CoinStart_WhenCallerCancelsAfterSuccessfulPurchase_StillMarksProgress()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.Coins, 100);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            PuzzleStartService service = CreateService(
                new CoinPuzzleStartModeHandler(
                    progressRepository,
                    new FakePurchaseService(PurchaseResult.Success(), cancellation.Cancel)));

            try
            {
                PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, cancellation.Token);

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Not.Null);
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        [Test]
        public async Task CoinStart_WhenPurchaseFails_DoesNotMarkProgress()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.Coins, 100);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new CoinPuzzleStartModeHandler(
                    progressRepository,
                    new FakePurchaseService(PurchaseResult.Failure(PurchaseFailureReason.InsufficientFunds))));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(PuzzleStartFailureReason.NotEnoughCoins));
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Null);
        }

        [TestCase(PurchaseFailureReason.InsufficientFunds, PuzzleStartFailureReason.NotEnoughCoins)]
        [TestCase(PurchaseFailureReason.Unavailable, PuzzleStartFailureReason.PurchaseUnavailable)]
        [TestCase(PurchaseFailureReason.Cancelled, PuzzleStartFailureReason.PurchaseCancelled)]
        public async Task CoinStart_MapsPurchaseFailureReason(
            PurchaseFailureReason purchaseFailureReason,
            PuzzleStartFailureReason expectedFailureReason)
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.Coins, 100);
            PuzzleStartService service = CreateService(
                new CoinPuzzleStartModeHandler(
                    new FakeProgressRepository(),
                    new FakePurchaseService(PurchaseResult.Failure(purchaseFailureReason))));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expectedFailureReason));
        }

        [Test]
        public async Task RewardedAdStart_WhenAdCompletes_MarksProgress()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.RewardedAd, 0);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new RewardedAdPuzzleStartModeHandler(
                    progressRepository,
                    new FakeAdService(AdAvailability.Available(), AdShowResult.Completed())));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Not.Null);
        }

        [Test]
        public async Task RewardedAdStart_WhenCallerCancelsAfterCompletedAd_StillMarksProgress()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.RewardedAd, 0);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            PuzzleStartService service = CreateService(
                new RewardedAdPuzzleStartModeHandler(
                    progressRepository,
                    new FakeAdService(
                        AdAvailability.Available(),
                        AdShowResult.Completed(),
                        cancellation.Cancel)));

            try
            {
                PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, cancellation.Token);

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Not.Null);
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        [TestCase(AdShowFailureReason.WeakInternet, PuzzleStartFailureReason.WeakInternet)]
        [TestCase(AdShowFailureReason.NotReady, PuzzleStartFailureReason.AdUnavailable)]
        public async Task RewardedAdStart_WhenAdUnavailable_MapsFailure(
            AdShowFailureReason adFailureReason,
            PuzzleStartFailureReason expectedFailureReason)
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.RewardedAd, 0);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new RewardedAdPuzzleStartModeHandler(
                    progressRepository,
                    new FakeAdService(
                        AdAvailability.Unavailable(adFailureReason),
                        AdShowResult.Completed())));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expectedFailureReason));
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Null);
        }

        [TestCase(AdShowFailureReason.Skipped, PuzzleStartFailureReason.AdSkipped)]
        [TestCase(AdShowFailureReason.Failed, PuzzleStartFailureReason.AdUnavailable)]
        public async Task RewardedAdStart_WhenShowFails_MapsFailure(
            AdShowFailureReason adFailureReason,
            PuzzleStartFailureReason expectedFailureReason)
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.RewardedAd, 0);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new RewardedAdPuzzleStartModeHandler(
                    progressRepository,
                    new FakeAdService(
                        AdAvailability.Available(),
                        AdShowResult.Failure(adFailureReason))));

            PuzzleStartAttempt result = await service.StartNewAsync(puzzle, 36, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expectedFailureReason));
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Null);
        }

        [Test]
        public void StartNew_WhenTokenCanceled_DoesNotMarkProgress()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.Free, 0);
            FakeProgressRepository progressRepository = new FakeProgressRepository();
            PuzzleStartService service = CreateService(
                new FreePuzzleStartModeHandler(progressRepository));
            CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.CatchAsync<OperationCanceledException>(
                async () => await service.StartNewAsync(puzzle, 36, cancellation.Token));
            Assert.That(progressRepository.GetProgress(puzzle.Id, 36), Is.Null);

            cancellation.Dispose();
        }

        [Test]
        public void StartNew_WhenHandlerIsMissing_ThrowsConfigurationError()
        {
            PuzzleDefinition puzzle = CreatePuzzle(PuzzleStartMode.RewardedAd, 0);
            PuzzleStartService service = CreateService(
                new FreePuzzleStartModeHandler(new FakeProgressRepository()));

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.StartNewAsync(puzzle, 36, CancellationToken.None));
        }

        [Test]
        public void Constructor_WhenHandlerIsDuplicated_ThrowsConfigurationError()
        {
            Assert.Throws<InvalidOperationException>(() => CreateService(
                new FreePuzzleStartModeHandler(new FakeProgressRepository()),
                new FreePuzzleStartModeHandler(new FakeProgressRepository())));
        }

        private static PuzzleStartService CreateService(params IPuzzleStartModeHandler[] handlers)
        {
            return new PuzzleStartService(new PuzzleCutStartOptionResolver(), handlers);
        }

        private static PuzzleDefinition CreatePuzzle(PuzzleStartMode mode, int coinPrice)
        {
            return new PuzzleDefinition(
                new PuzzleId("test-puzzle"),
                "Test Puzzle",
                "Tests",
                new[]
                {
                    new PuzzleCutDefinition(36, new StartOption(mode, coinPrice))
                });
        }

        private sealed class FakeProgressRepository : IPuzzleProgressRepository
        {
            private readonly Dictionary<string, PuzzleProgress> progressByKey = new Dictionary<string, PuzzleProgress>();

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

        private sealed class FakePurchaseService : IPurchaseService
        {
            private readonly PurchaseResult result;
            private readonly Action onPurchase;

            public FakePurchaseService(PurchaseResult result, Action onPurchase = null)
            {
                this.result = result;
                this.onPurchase = onPurchase;
            }

            public int GetBalance(string currencyCode)
            {
                return 0;
            }

            public Task<PurchaseResult> PurchaseAsync(PurchaseRequest request, CancellationToken cancellationToken)
            {
                onPurchase?.Invoke();
                return Task.FromResult(result);
            }
        }

        private sealed class FakeAdService : IAdService
        {
            private readonly AdAvailability availability;
            private readonly AdShowResult result;
            private readonly Action onShow;

            public FakeAdService(AdAvailability availability, AdShowResult result, Action onShow = null)
            {
                this.availability = availability;
                this.result = result;
                this.onShow = onShow;
            }

            public AdAvailability GetRewardedAvailability(AdPlacement placement)
            {
                return availability;
            }

            public Task<AdShowResult> ShowRewardedAsync(AdPlacement placement, CancellationToken cancellationToken)
            {
                onShow?.Invoke();
                return Task.FromResult(result);
            }
        }
    }
}
