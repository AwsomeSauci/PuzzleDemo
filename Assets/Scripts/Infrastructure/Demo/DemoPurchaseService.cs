using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Application;

namespace PuzzleFlow.Infrastructure
{
    public enum DemoPurchaseMode
    {
        SucceedWhenAffordable,
        AlwaysInsufficientFunds,
        Unavailable,
        Cancelled
    }

    public sealed class DemoPurchaseService : IPurchaseService
    {
        private readonly DemoPurchaseMode mode;
        private int balance;

        public DemoPurchaseService(int startingBalance, DemoPurchaseMode mode)
        {
            balance = startingBalance < 0 ? 0 : startingBalance;
            this.mode = mode;
        }

        public int GetBalance(string currencyCode)
        {
            return balance;
        }

        public Task<PurchaseResult> PurchaseAsync(PurchaseRequest request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<PurchaseResult>(cancellationToken);
            }

            switch (mode)
            {
                case DemoPurchaseMode.SucceedWhenAffordable:
                    return Task.FromResult(PurchaseIfAffordable(request));
                case DemoPurchaseMode.Unavailable:
                    return Task.FromResult(PurchaseResult.Failure(PurchaseFailureReason.Unavailable));
                case DemoPurchaseMode.Cancelled:
                    return Task.FromResult(PurchaseResult.Failure(PurchaseFailureReason.Cancelled));
                default:
                    return Task.FromResult(PurchaseResult.Failure(PurchaseFailureReason.InsufficientFunds));
            }
        }

        private PurchaseResult PurchaseIfAffordable(PurchaseRequest request)
        {
            if (request == null || request.Price <= 0)
            {
                return PurchaseResult.Failure(PurchaseFailureReason.Unavailable);
            }

            if (balance < request.Price)
            {
                return PurchaseResult.Failure(PurchaseFailureReason.InsufficientFunds);
            }

            balance -= request.Price;
            return PurchaseResult.Success();
        }
    }
}
