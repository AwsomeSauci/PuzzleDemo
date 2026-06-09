using PuzzleFlow.Presentation.Navigation;
using System;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Presentation.Popups
{
    public sealed class UniversalPopupFragment : FragmentBase<PopupRequest, PopupResult>
    {
        [SerializeField] private Transform popupRoot;

        private DiContainer container;
        private UniversalPopupView activePopupView;
        private PopupRequest activeRequest;

        [Inject]
        private void Construct(DiContainer container)
        {
            this.container = container;
        }

        protected override void OnOpen(PopupRequest args)
        {
            activeRequest = args;
            RenderPopup(args);
        }

        protected override void OnClose()
        {
            ClearActivePopup();
            activeRequest = null;
        }

        private void RenderPopup(PopupRequest request)
        {
            ClearActivePopup();

            if (request.Prefab == null)
            {
                throw new InvalidOperationException($"Popup '{request.PopupId}' has no prefab.");
            }

            Transform root = popupRoot != null ? popupRoot : transform;
            if (container == null)
            {
                throw new InvalidOperationException($"{nameof(UniversalPopupFragment)} was not injected.");
            }

            activePopupView = container.InstantiatePrefabForComponent<UniversalPopupView>(request.Prefab, root);
            activePopupView.gameObject.SetActive(true);
            activePopupView.Bind(request, CompleteFromAction);
        }

        private void ClearActivePopup()
        {
            if (activePopupView != null)
            {
                Destroy(activePopupView.gameObject);
                activePopupView = null;
            }
        }

        private void CompleteFromAction(PopupActionDefinition action)
        {
            Close(PopupResult.FromAction(activeRequest?.PopupId, action));
        }
    }
}
