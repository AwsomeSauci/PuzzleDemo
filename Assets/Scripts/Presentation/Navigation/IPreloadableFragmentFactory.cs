using UnityEngine;

namespace PuzzleFlow.Presentation.Navigation
{
    public interface IPreloadableFragmentFactory
    {
        void PreloadAll(Transform screenRoot, Transform overlayRoot);
    }
}
