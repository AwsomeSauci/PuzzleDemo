using NUnit.Framework;
using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using PuzzleFlow.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PuzzleFlow.Tests
{
    public sealed class DemoServicesTests
    {
        [Test]
        public void PuzzleId_WhenValueIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PuzzleId(null));
        }

        [Test]
        public void PuzzleId_WhenValueIsWhitespace_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleId(" "));
        }

        [Test]
        public void ProgressRepository_ParameterlessConstructor_StartsEmpty()
        {
            InMemoryPuzzleProgressRepository repository = new InMemoryPuzzleProgressRepository();

            Assert.That(repository.GetProgress(new PuzzleId("missing"), 36), Is.Null);
        }

        [Test]
        public void ProgressRepository_WhenSeedsAreNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new InMemoryPuzzleProgressRepository(null));
        }

        [Test]
        public async Task DemoPurchase_SucceedWhenAffordable_DeductsBalance()
        {
            DemoPurchaseService service = new DemoPurchaseService(
                150,
                DemoPurchaseMode.SucceedWhenAffordable);

            PurchaseResult result = await service.PurchaseAsync(
                new PurchaseRequest("test", CurrencyCodes.Coins, 100),
                CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.GetBalance(CurrencyCodes.Coins), Is.EqualTo(50));
        }

        [Test]
        public async Task DemoPurchase_SucceedWhenAffordable_WhenBalanceIsLow_FailsWithoutDeduction()
        {
            DemoPurchaseService service = new DemoPurchaseService(
                50,
                DemoPurchaseMode.SucceedWhenAffordable);

            PurchaseResult result = await service.PurchaseAsync(
                new PurchaseRequest("test", CurrencyCodes.Coins, 100),
                CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(PurchaseFailureReason.InsufficientFunds));
            Assert.That(service.GetBalance(CurrencyCodes.Coins), Is.EqualTo(50));
        }

        [TestCase(DemoPurchaseMode.AlwaysInsufficientFunds, PurchaseFailureReason.InsufficientFunds)]
        [TestCase(DemoPurchaseMode.Unavailable, PurchaseFailureReason.Unavailable)]
        [TestCase(DemoPurchaseMode.Cancelled, PurchaseFailureReason.Cancelled)]
        public async Task DemoPurchase_ExplicitFailureModes_MapToPurchaseFailure(
            DemoPurchaseMode mode,
            PurchaseFailureReason expectedFailure)
        {
            DemoPurchaseService service = new DemoPurchaseService(150, mode);

            PurchaseResult result = await service.PurchaseAsync(
                new PurchaseRequest("test", CurrencyCodes.Coins, 100),
                CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expectedFailure));
        }

        [Test]
        public async Task DemoAd_CompletedMode_IsAvailableAndCompletes()
        {
            DemoAdService service = new DemoAdService(DemoRewardedAdMode.Completed);

            AdAvailability availability = service.GetRewardedAvailability(AdPlacement.PuzzleStartReward);
            AdShowResult result = await service.ShowRewardedAsync(
                AdPlacement.PuzzleStartReward,
                CancellationToken.None);

            Assert.That(availability.IsAvailable, Is.True);
            Assert.That(result.IsCompleted, Is.True);
        }

        [Test]
        public async Task DemoAd_SkippedMode_IsAvailableButShowFailsAsSkipped()
        {
            DemoAdService service = new DemoAdService(DemoRewardedAdMode.Skipped);

            AdAvailability availability = service.GetRewardedAvailability(AdPlacement.PuzzleStartReward);
            AdShowResult result = await service.ShowRewardedAsync(
                AdPlacement.PuzzleStartReward,
                CancellationToken.None);

            Assert.That(availability.IsAvailable, Is.True);
            Assert.That(result.IsCompleted, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(AdShowFailureReason.Skipped));
        }

        [Test]
        public void DemoAd_WeakInternetMode_IsUnavailableBeforeShow()
        {
            DemoAdService service = new DemoAdService(DemoRewardedAdMode.WeakInternet);

            AdAvailability availability = service.GetRewardedAvailability(AdPlacement.PuzzleStartReward);

            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(availability.FailureReason, Is.EqualTo(AdShowFailureReason.WeakInternet));
        }
    }
}
