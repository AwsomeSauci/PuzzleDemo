namespace PuzzleFlow.Presentation.Navigation
{
    public static class FragmentIds
    {
        public const string Gallery = "fragment.gallery";
        public const string PuzzlePreview = "fragment.puzzle-preview";
        public const string UniversalPopup = "fragment.universal-popup";
    }

    public enum FragmentLayer
    {
        Screen,
        Overlay
    }

    public readonly struct Unit
    {
        public static readonly Unit Value = new Unit();
    }
}
