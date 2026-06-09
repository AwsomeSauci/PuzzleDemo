using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Presentation.Popups
{
    public sealed class DialogInvoker : MonoBehaviour
    {
        [SerializeField] private PopupDefinition popupDefinition;

        private IUniversalPopupService popupService;

        [Inject]
        private void Construct(IUniversalPopupService popupService)
        {
            this.popupService = popupService;
        }

        public void Invoke()
        {
            if (popupService == null || popupDefinition == null)
            {
                return;
            }

            popupService.ShowAsync(popupDefinition, destroyCancellationToken).Forget();
        }
    }
}
