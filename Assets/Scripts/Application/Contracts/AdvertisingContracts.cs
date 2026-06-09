namespace PuzzleFlow.Application
{
    public enum AdPlacement
    {
        PuzzleStartReward
    }

    public enum AdShowFailureReason
    {
        None,
        WeakInternet,
        NotReady,
        Skipped,
        Failed
    }

    public sealed class AdShowResult
    {
        private AdShowResult(bool isCompleted, AdShowFailureReason failureReason)
        {
            IsCompleted = isCompleted;
            FailureReason = failureReason;
        }

        public bool IsCompleted { get; }
        public AdShowFailureReason FailureReason { get; }

        public static AdShowResult Completed()
        {
            return new AdShowResult(true, AdShowFailureReason.None);
        }

        public static AdShowResult Failure(AdShowFailureReason reason)
        {
            return new AdShowResult(false, reason);
        }
    }

    public sealed class AdAvailability
    {
        private AdAvailability(bool isAvailable, AdShowFailureReason failureReason)
        {
            IsAvailable = isAvailable;
            FailureReason = failureReason;
        }

        public bool IsAvailable { get; }
        public AdShowFailureReason FailureReason { get; }

        public static AdAvailability Available()
        {
            return new AdAvailability(true, AdShowFailureReason.None);
        }

        public static AdAvailability Unavailable(AdShowFailureReason reason)
        {
            return new AdAvailability(false, reason);
        }
    }
}