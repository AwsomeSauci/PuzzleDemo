using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.Virtualization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PuzzleFlow.Presentation.Gallery
{
    public sealed class PuzzleTileCell : MonoBehaviour, IVirtualizedGridCell<PuzzleGalleryItemViewModel>
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image preview;
        [SerializeField] private PuzzleIdUnityEvent clicked = new PuzzleIdUnityEvent();

        private PuzzleGalleryItemViewModel item;

        public RectTransform RectTransform => rectTransform != null
            ? rectTransform
            : (RectTransform)transform;
        public UnityEvent<PuzzleId> Clicked => clicked;

        public void Bind(PuzzleGalleryItemViewModel item, int itemIndex)
        {
            if (preview == null)
            {
                throw new System.InvalidOperationException($"{nameof(PuzzleTileCell)} on '{name}' has no preview image reference.");
            }

            this.item = item;
            preview.sprite = item.Preview;
            preview.enabled = item.Preview != null;
        }

        public void Unbind()
        {
            item = null;
            if (preview != null)
            {
                preview.sprite = null;
                preview.enabled = false;
            }
        }

        public void OnClicked()
        {
            if (item != null)
            {
                clicked.Invoke(item.Puzzle.Id);
            }
        }
    }
}
