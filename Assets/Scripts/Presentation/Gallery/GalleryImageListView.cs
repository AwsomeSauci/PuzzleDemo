using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.Virtualization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PuzzleFlow.Presentation.Gallery
{
    public sealed class GalleryImageListView : MonoBehaviour, IPuzzleGalleryView
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private PuzzleTileCell cellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(830f, 830f);
        [SerializeField] private Vector2 spacing = new Vector2(40f, 40f);
        [SerializeField] private RectOffset padding;
        [SerializeField] private int bufferRows = 1;
        [SerializeField] private PuzzleIdUnityEvent itemClicked = new PuzzleIdUnityEvent();

        private VirtualizedGridScrollAdapter<PuzzleGalleryItemViewModel, PuzzleTileCell> adapter;

        public UnityEvent<PuzzleId> ItemClicked => itemClicked;
        public event Action<int> ItemBecameVisible;

        public void Initialize()
        {
            EnsureAdapter();
        }

        public void Render(IReadOnlyList<PuzzleGalleryItemViewModel> items)
        {
            EnsureAdapter();
            adapter.SetItems(items);
        }

        public void UpdateItem(int itemIndex, PuzzleGalleryItemViewModel item)
        {
            adapter?.UpdateItem(itemIndex, item);
        }

        public void Clear()
        {
            adapter?.ClearItems();
        }

        public void DisposeAdapter()
        {
            if (adapter != null)
            {
                adapter.ItemBecameVisible -= OnItemBecameVisible;
                adapter.Dispose();
            }

            adapter = null;
        }

        private void Update()
        {
            adapter?.RefreshIfNeeded();
        }

        private void OnDestroy()
        {
            DisposeAdapter();
        }

        private void EnsureAdapter()
        {
            if (adapter != null)
            {
                return;
            }

            adapter = new VirtualizedGridScrollAdapter<PuzzleGalleryItemViewModel, PuzzleTileCell>(
                scrollRect,
                content,
                CreateCell,
                cellSize,
                spacing,
                padding ?? new RectOffset(18, 18, 18, 18),
                bufferRows);
            adapter.ItemBecameVisible += OnItemBecameVisible;
        }

        private PuzzleTileCell CreateCell()
        {
            PuzzleTileCell cell = Instantiate(cellPrefab, content);
            cell.Clicked.AddListener(OnCellClicked);
            return cell;
        }

        private void OnCellClicked(PuzzleId puzzleId)
        {
            itemClicked.Invoke(puzzleId);
        }

        private void OnItemBecameVisible(int itemIndex)
        {
            ItemBecameVisible?.Invoke(itemIndex);
        }
    }
}
