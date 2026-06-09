namespace PuzzleFlow.Presentation.Navigation
{
    public interface IRoutableFragment
    {
        void Open(object args, IFragmentController controller);
        void Rebuild(object args);
    }
}
