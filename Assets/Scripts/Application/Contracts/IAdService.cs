using System.Threading;
using System.Threading.Tasks;

namespace PuzzleFlow.Application
{
    public interface IAdService
    {
        AdAvailability GetRewardedAvailability(AdPlacement placement);
        Task<AdShowResult> ShowRewardedAsync(AdPlacement placement, CancellationToken cancellationToken);
    }
}