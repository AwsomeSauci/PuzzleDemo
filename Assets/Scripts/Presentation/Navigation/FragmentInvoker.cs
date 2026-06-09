using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Presentation.Navigation
{
    public sealed class FragmentInvoker : MonoBehaviour
    {
        public enum InvocationType
        {
            Open,
            Close,
            Rebuild,
            CloseTop
        }

        [SerializeField] private string fragmentId;
        [SerializeField] private InvocationType invocationType = InvocationType.Open;
        [SerializeField] private bool invocationEnabled = true;

        private IFragmentRouter fragmentRouter;

        public string FragmentId => fragmentId;

        [Inject]
        public void Construct(IFragmentRouter fragmentRouter)
        {
            this.fragmentRouter = fragmentRouter;
        }

        public void Invoke()
        {
            if (!invocationEnabled || fragmentRouter == null)
            {
                return;
            }

            switch (invocationType)
            {
                case InvocationType.Open:
                    fragmentRouter.ShowAsync<Unit, Unit>(fragmentId, Unit.Value).Forget();
                    break;
                case InvocationType.Close:
                    fragmentRouter.TryClose(fragmentId);
                    break;
                case InvocationType.Rebuild:
                    fragmentRouter.TryRebuild(fragmentId, Unit.Value);
                    break;
                case InvocationType.CloseTop:
                    fragmentRouter.TryCloseTop();
                    break;
            }
        }

        public void SetInvocationEnabled(bool isEnabled)
        {
            invocationEnabled = isEnabled;
        }
    }
}
