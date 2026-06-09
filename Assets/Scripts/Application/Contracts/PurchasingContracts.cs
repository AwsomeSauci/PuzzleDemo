namespace PuzzleFlow.Application
{
    public enum PurchaseFailureReason
    {
        None,
        InsufficientFunds,
        Unavailable,
        Cancelled
    }

    public sealed class PurchaseRequest
    {
        public PurchaseRequest(string productId, string currencyCode, int price)
        {
            ProductId = productId;
            CurrencyCode = currencyCode;
            Price = price;
        }

        public string ProductId { get; }
        public string CurrencyCode { get; }
        public int Price { get; }
    }

    public sealed class PurchaseResult
    {
        private PurchaseResult(bool isSuccess, PurchaseFailureReason failureReason)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason;
        }

        public bool IsSuccess { get; }
        public PurchaseFailureReason FailureReason { get; }

        public static PurchaseResult Success()
        {
            return new PurchaseResult(true, PurchaseFailureReason.None);
        }

        public static PurchaseResult Failure(PurchaseFailureReason reason)
        {
            return new PurchaseResult(false, reason);
        }
    }
}