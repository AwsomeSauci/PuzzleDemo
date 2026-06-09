using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleFlow.Presentation.Popups
{
    public sealed class PopupActionButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private PopupActionDefinition action;
        private Action<PopupActionDefinition> clicked;

        public void Bind(PopupActionDefinition action, Action<PopupActionDefinition> clicked)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.clicked = clicked ?? throw new ArgumentNullException(nameof(clicked));

            if (label != null)
            {
                label.text = action.Label;
            }

            if (button == null)
            {
                throw new InvalidOperationException($"Popup action button '{name}' has no Button reference.");
            }

            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        public void Unbind()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }

            action = null;
            clicked = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnClicked()
        {
            clicked?.Invoke(action);
        }
    }
}
