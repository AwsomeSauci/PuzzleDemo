using NUnit.Framework;
using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.PuzzlePreview;

namespace PuzzleFlow.Tests
{
    public sealed class PuzzleStartOptionPresentationTests
    {
        [Test]
        public void CreateStartButton_MapsModeToEnumState()
        {
            PuzzleStartOptionViewModelFactory factory = new PuzzleStartOptionViewModelFactory(
                new IStartOptionPresentationStrategy[]
                {
                    new FreeStartOptionPresentationStrategy(),
                    new CoinStartOptionPresentationStrategy(),
                    new RewardedAdStartOptionPresentationStrategy()
                });

            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Free, 0)).State,
                Is.EqualTo(PuzzleStartButtonState.Free));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Coins, 100)).State,
                Is.EqualTo(PuzzleStartButtonState.Coins));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.RewardedAd, 0)).State,
                Is.EqualTo(PuzzleStartButtonState.RewardedAd));
        }

        [Test]
        public void CreateStartButton_ProvidesResolvedLabelText()
        {
            PuzzleStartOptionViewModelFactory factory = new PuzzleStartOptionViewModelFactory(
                new IStartOptionPresentationStrategy[]
                {
                    new FreeStartOptionPresentationStrategy(),
                    new CoinStartOptionPresentationStrategy(),
                    new RewardedAdStartOptionPresentationStrategy()
                });

            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Free, 0)).LabelText,
                Is.EqualTo("Play"));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Coins, 100)).LabelText,
                Is.EqualTo("Play for 100 coins"));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.RewardedAd, 0)).LabelText,
                Is.EqualTo("Watch ad"));
        }

        [Test]
        public void CreateStartButton_UsesConfiguredCopy()
        {
            PuzzleStartOptionPresentationTextSet textSet = new PuzzleStartOptionPresentationTextSet(
                "Gratis",
                "Start",
                "{0} gold",
                "Start for {0} gold",
                "gold",
                "Video",
                "Watch video");
            PuzzleStartOptionViewModelFactory factory = new PuzzleStartOptionViewModelFactory(
                new IStartOptionPresentationStrategy[]
                {
                    new FreeStartOptionPresentationStrategy(textSet),
                    new CoinStartOptionPresentationStrategy(textSet),
                    new RewardedAdStartOptionPresentationStrategy(textSet)
                });

            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Free, 0)).LabelText,
                Is.EqualTo("Start"));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Coins, 100)).LabelText,
                Is.EqualTo("Start for 100 gold"));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.Coins, 100)).CurrencyCode,
                Is.EqualTo("gold"));
            Assert.That(
                factory.CreateStartButton(new StartOption(PuzzleStartMode.RewardedAd, 0)).LabelText,
                Is.EqualTo("Watch video"));
        }
    }
}
