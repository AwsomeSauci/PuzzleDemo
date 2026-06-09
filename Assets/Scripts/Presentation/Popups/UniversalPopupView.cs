using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PuzzleFlow.Presentation.Popups
{
    public sealed class UniversalPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text message;
        [SerializeField] private PopupActionButtonView[] actionButtons;

        private Action<PopupActionDefinition> clicked;

        public void Bind(PopupRequest request, Action<PopupActionDefinition> clicked)
        {
            this.clicked = clicked ?? throw new ArgumentNullException(nameof(clicked));

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (title != null)
            {
                title.text = request.Message.Title;
            }

            if (message != null)
            {
                message.text = request.Message.Body;
            }

            IReadOnlyList<PopupActionDefinition> actions = PopupActionResolver.Resolve(request);
            if (actionButtons == null || actionButtons.Length < actions.Count)
            {
                throw new InvalidOperationException(
                    $"Popup view '{name}' has {actionButtons?.Length ?? 0} action buttons, but request requires {actions.Count}.");
            }

            for (int index = 0; index < actionButtons.Length; index++)
            {
                PopupActionButtonView button = actionButtons[index];
                if (button == null)
                {
                    throw new InvalidOperationException($"Popup view '{name}' has null action button at index {index}.");
                }

                bool isVisible = index < actions.Count;
                button.gameObject.SetActive(isVisible);
                if (isVisible)
                {
                    button.Bind(actions[index], OnActionClicked);
                }
                else
                {
                    button.Unbind();
                }
            }
        }

        private void OnDestroy()
        {
            if (actionButtons == null)
            {
                return;
            }

            for (int index = 0; index < actionButtons.Length; index++)
            {
                actionButtons[index]?.Unbind();
            }
        }

        private void OnActionClicked(PopupActionDefinition action)
        {
            clicked?.Invoke(action);
        }
    }
}
