using System.Threading;
using System.Threading.Tasks;

namespace PuzzleFlow.Application
{
    public interface IPurchaseService
    {
        int GetBalance(string currencyCode);
        Task<PurchaseResult> PurchaseAsync(PurchaseRequest request, CancellationToken cancellationToken);
    }
}