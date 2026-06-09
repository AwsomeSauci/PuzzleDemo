using UnityEngine;

namespace PuzzleFlow.Presentation.Navigation
{
    public interface IFragmentFactory
    {
        FragmentLayer GetLayer(string fragmentId);
        FragmentLayer GetLayer(FragmentId fragmentId);
        IRoutableFragment GetOrCreate(string fragmentId, Transform parent, int sortingOrder);
        IRoutableFragment GetOrCreate(FragmentId fragmentId, Transform parent, int sortingOrder);
    }
}
