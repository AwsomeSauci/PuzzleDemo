namespace PuzzleFlow.Presentation.Navigation
{
    public readonly struct FragmentRoute
    {
        public FragmentRoute(FragmentId fragmentId)
        {
            FragmentId = fragmentId;
        }

        public FragmentId FragmentId { get; }
        public bool IsEmpty => FragmentId.IsEmpty;

        public static FragmentRoute Open(FragmentId fragmentId)
        {
            return new FragmentRoute(fragmentId);
        }

        public override string ToString()
        {
            return FragmentId.ToString();
        }
    }

    public readonly struct FragmentRoute<TArgs, TResult>
    {
        public FragmentRoute(FragmentId fragmentId)
        {
            FragmentId = fragmentId;
        }

        public FragmentId FragmentId { get; }
        public bool IsEmpty => FragmentId.IsEmpty;

        public override string ToString()
        {
            return FragmentId.ToString();
        }
    }
}

