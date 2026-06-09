using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Application;

namespace PuzzleFlow.Infrastructure
{
    public enum DemoRewardedAdMode
    {
        Completed,
        Unavailable,
        WeakInternet,
        Skipped,
        Failed
    }

    public sealed class DemoAdService : IAdService
    {
        private readonly DemoRewardedAdMode mode;

        public DemoAdService(DemoRewardedAdMode mode)
        {
            this.mode = mode;
        }

        public AdAvailability GetRewardedAvailability(AdPlacement placement)
        {
            switch (mode)
            {
                case DemoRewardedAdMode.Unavailable:
                    return AdAvailability.Unavailable(AdShowFailureReason.NotReady);
                case DemoRewardedAdMode.WeakInternet:
                    return AdAvailability.Unavailable(AdShowFailureReason.WeakInternet);
                default:
                    return AdAvailability.Available();
            }
        }

        public Task<AdShowResult> ShowRewardedAsync(AdPlacement placement, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<AdShowResult>(cancellationToken);
            }

            switch (mode)
            {
                case DemoRewardedAdMode.Unavailable:
                    return Task.FromResult(AdShowResult.Failure(AdShowFailureReason.NotReady));
                case DemoRewardedAdMode.WeakInternet:
                    return Task.FromResult(AdShowResult.Failure(AdShowFailureReason.WeakInternet));
                case DemoRewardedAdMode.Skipped:
                    return Task.FromResult(AdShowResult.Failure(AdShowFailureReason.Skipped));
                case DemoRewardedAdMode.Failed:
                    return Task.FromResult(AdShowResult.Failure(AdShowFailureReason.Failed));
                default:
                    return Task.FromResult(AdShowResult.Completed());
            }
        }
    }
}
