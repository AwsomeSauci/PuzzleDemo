using PuzzleFlow.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzleStartButtonViewModel
    {
        public static readonly PuzzleStartButtonViewModel None =
            new PuzzleStartButtonViewModel(PuzzleStartButtonState.None, 0, string.Empty, string.Empty);

        public PuzzleStartButtonViewModel(
            PuzzleStartButtonState state,
            int coinPrice = 0,
            string currencyCode = "",
            string labelText = "")
        {
            State = state;
            CoinPrice = coinPrice;
            CurrencyCode = currencyCode ?? string.Empty;
            LabelText = labelText ?? string.Empty;
        }

        public PuzzleStartButtonState State { get; }
        public int CoinPrice { get; }
        public string CurrencyCode { get; }
        public string LabelText { get; }
    }

    public sealed class PuzzleCutOptionViewModel
    {
        public PuzzleCutOptionViewModel(int pieceCount, string label, bool isSelected)
        {
            PieceCount = pieceCount;
            Label = label;
            IsSelected = isSelected;
        }

        public int PieceCount { get; }
        public string Label { get; }
        public bool IsSelected { get; }
    }

    [Serializable]
    public sealed class PuzzleStartOptionPresentationTextSet
    {
        public const string DefaultFreeCutLabel = "Free";
        public const string DefaultFreeStartLabel = "Play";
        public const string DefaultCoinCutFormat = "{0} coins";
        public const string DefaultCoinStartFormat = "Play for {0} coins";
        public const string DefaultRewardedAdCutLabel = "Ad";
        public const string DefaultRewardedAdStartLabel = "Watch ad";
        public const string DefaultCoinCurrencyCode = "coins";

        public static readonly PuzzleStartOptionPresentationTextSet Default =
            new PuzzleStartOptionPresentationTextSet();

        public PuzzleStartOptionPresentationTextSet(
            string freeCutLabel = DefaultFreeCutLabel,
            string freeStartLabel = DefaultFreeStartLabel,
            string coinCutFormat = DefaultCoinCutFormat,
            string coinStartFormat = DefaultCoinStartFormat,
            string coinCurrencyCode = DefaultCoinCurrencyCode,
            string rewardedAdCutLabel = DefaultRewardedAdCutLabel,
            string rewardedAdStartLabel = DefaultRewardedAdStartLabel)
        {
            FreeCutLabel = Resolve(freeCutLabel, DefaultFreeCutLabel);
            FreeStartLabel = Resolve(freeStartLabel, DefaultFreeStartLabel);
            CoinCutFormat = Resolve(coinCutFormat, DefaultCoinCutFormat);
            CoinStartFormat = Resolve(coinStartFormat, DefaultCoinStartFormat);
            CoinCurrencyCode = Resolve(coinCurrencyCode, DefaultCoinCurrencyCode);
            RewardedAdCutLabel = Resolve(rewardedAdCutLabel, DefaultRewardedAdCutLabel);
            RewardedAdStartLabel = Resolve(rewardedAdStartLabel, DefaultRewardedAdStartLabel);
        }

        public string FreeCutLabel { get; }
        public string FreeStartLabel { get; }
        public string CoinCutFormat { get; }
        public string CoinStartFormat { get; }
        public string CoinCurrencyCode { get; }
        public string RewardedAdCutLabel { get; }
        public string RewardedAdStartLabel { get; }

        private static string Resolve(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }


    public sealed class PuzzleStartOptionViewModelFactory : IPuzzleStartOptionViewModelFactory
    {
        private readonly Dictionary<PuzzleStartMode, IStartOptionPresentationStrategy> strategiesByMode =
            new Dictionary<PuzzleStartMode, IStartOptionPresentationStrategy>();

        public PuzzleStartOptionViewModelFactory(IEnumerable<IStartOptionPresentationStrategy> strategies)
        {
            if (strategies == null)
            {
                throw new ArgumentNullException(nameof(strategies));
            }

            foreach (IStartOptionPresentationStrategy strategy in strategies)
            {
                strategiesByMode[strategy.Mode] = strategy;
            }
        }

        public PuzzleStartButtonViewModel CreateStartButton(StartOption option)
        {
            return Resolve(option.Mode).CreateStartButton(option);
        }

        public PuzzleCutOptionViewModel CreateCutOption(PuzzleCutDefinition option, bool isSelected)
        {
            return Resolve(option.StartOption.Mode).CreateCutOption(option, isSelected);
        }

        private IStartOptionPresentationStrategy Resolve(PuzzleStartMode mode)
        {
            if (strategiesByMode.TryGetValue(mode, out IStartOptionPresentationStrategy strategy))
            {
                return strategy;
            }

            throw new InvalidOperationException($"Start option presentation for '{mode}' is not configured.");
        }
    }


    public abstract class StartOptionPresentationStrategyBase : IStartOptionPresentationStrategy
    {
        private readonly string cutLabel;
        private readonly string startButtonLabel;
        private readonly PuzzleStartButtonState buttonState;

        protected StartOptionPresentationStrategyBase(
            PuzzleStartButtonState buttonState,
            string cutLabel,
            string startButtonLabel)
        {
            this.cutLabel = cutLabel;
            this.startButtonLabel = startButtonLabel;
            this.buttonState = buttonState;
        }

        public abstract PuzzleStartMode Mode { get; }

        public virtual PuzzleStartButtonViewModel CreateStartButton(StartOption option)
        {
            return new PuzzleStartButtonViewModel(buttonState, labelText: BuildStartButtonLabel(option));
        }

        public virtual PuzzleCutOptionViewModel CreateCutOption(PuzzleCutDefinition option, bool isSelected)
        {
            return new PuzzleCutOptionViewModel(
                option.PieceCount,
                option.PieceCount + "\n" + BuildCutLabel(option.StartOption),
                isSelected);
        }

        protected virtual string BuildCutLabel(StartOption option)
        {
            return cutLabel;
        }

        protected virtual string BuildStartButtonLabel(StartOption option)
        {
            return startButtonLabel;
        }
    }

    public sealed class FreeStartOptionPresentationStrategy : StartOptionPresentationStrategyBase
    {
        public FreeStartOptionPresentationStrategy(PuzzleStartOptionPresentationTextSet textSet)
            : base(
                PuzzleStartButtonState.Free,
                (textSet ?? PuzzleStartOptionPresentationTextSet.Default).FreeCutLabel,
                (textSet ?? PuzzleStartOptionPresentationTextSet.Default).FreeStartLabel)
        {
        }

        public FreeStartOptionPresentationStrategy()
            : this(PuzzleStartOptionPresentationTextSet.Default)
        {
        }

        public override PuzzleStartMode Mode => PuzzleStartMode.Free;
    }

    public sealed class CoinStartOptionPresentationStrategy : StartOptionPresentationStrategyBase
    {
        private readonly PuzzleStartOptionPresentationTextSet textSet;

        public CoinStartOptionPresentationStrategy(PuzzleStartOptionPresentationTextSet textSet)
            : base(PuzzleStartButtonState.Coins, string.Empty, string.Empty)
        {
            this.textSet = textSet ?? PuzzleStartOptionPresentationTextSet.Default;
        }

        public CoinStartOptionPresentationStrategy()
            : this(PuzzleStartOptionPresentationTextSet.Default)
        {
        }

        public override PuzzleStartMode Mode => PuzzleStartMode.Coins;

        public override PuzzleStartButtonViewModel CreateStartButton(StartOption option)
        {
            return new PuzzleStartButtonViewModel(
                PuzzleStartButtonState.Coins,
                option.CoinPrice,
                textSet.CoinCurrencyCode,
                string.Format(
                    CultureInfo.InvariantCulture,
                    textSet.CoinStartFormat,
                    option.CoinPrice));
        }

        protected override string BuildCutLabel(StartOption option)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                textSet.CoinCutFormat,
                option.CoinPrice);
        }
    }

    public sealed class RewardedAdStartOptionPresentationStrategy : StartOptionPresentationStrategyBase
    {
        public RewardedAdStartOptionPresentationStrategy(PuzzleStartOptionPresentationTextSet textSet)
            : base(
                PuzzleStartButtonState.RewardedAd,
                (textSet ?? PuzzleStartOptionPresentationTextSet.Default).RewardedAdCutLabel,
                (textSet ?? PuzzleStartOptionPresentationTextSet.Default).RewardedAdStartLabel)
        {
        }

        public RewardedAdStartOptionPresentationStrategy()
            : this(PuzzleStartOptionPresentationTextSet.Default)
        {
        }

        public override PuzzleStartMode Mode => PuzzleStartMode.RewardedAd;
    }
}
