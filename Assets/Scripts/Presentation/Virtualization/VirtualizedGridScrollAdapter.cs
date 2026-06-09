using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleFlow.Presentation.Virtualization
{
    public sealed class VirtualizedGridScrollAdapter<TItem, TCell> : IDisposable
        where TCell : IVirtualizedGridCell<TItem>
    {
        private readonly ScrollRect scrollRect;
        private readonly RectTransform viewport;
        private readonly RectTransform content;
        private readonly Func<TCell> createCell;
        private readonly Vector2 cellSize;
        private readonly Vector2 spacing;
        private readonly RectOffset padding;
        private readonly int bufferRows;
        private readonly Dictionary<int, TCell> visibleCellsByIndex = new Dictionary<int, TCell>();
        private readonly Stack<TCell> pool = new Stack<TCell>();
        private readonly List<int> itemsToRecycle = new List<int>();

        private readonly List<TItem> items = new List<TItem>();
        private int columns = 1;
        private float lastViewportWidth = -1f;
        private bool isDisposed;

        public event Action<int> ItemBecameVisible;

        public VirtualizedGridScrollAdapter(
            ScrollRect scrollRect,
            RectTransform content,
            Func<TCell> createCell,
            Vector2 cellSize,
            Vector2 spacing,
            RectOffset padding,
            int bufferRows = 1)
        {
            this.scrollRect = scrollRect;
            this.viewport = scrollRect.viewport;
            this.content = content;
            this.createCell = createCell;
            this.cellSize = cellSize;
            this.spacing = spacing;
            this.padding = padding;
            this.bufferRows = Mathf.Max(0, bufferRows);

            scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        public void SetItems(IReadOnlyList<TItem> nextItems)
        {
            items.Clear();
            if (nextItems != null)
            {
                for (int index = 0; index < nextItems.Count; index++)
                {
                    items.Add(nextItems[index]);
                }
            }

            scrollRect.verticalNormalizedPosition = 1f;
            RecycleAll();
            RebuildContentHeight();
            UpdateVisibleCells();
        }

        public void ClearItems()
        {
            if (isDisposed)
            {
                return;
            }

            RecycleAll();
            items.Clear();
            scrollRect.verticalNormalizedPosition = 1f;
            RebuildContentHeight();
        }

        public void UpdateItem(int itemIndex, TItem item)
        {
            if (itemIndex < 0 || itemIndex >= items.Count)
            {
                return;
            }

            items[itemIndex] = item;
            if (visibleCellsByIndex.TryGetValue(itemIndex, out TCell cell))
            {
                cell.Bind(item, itemIndex);
            }
        }

        public void RefreshIfNeeded()
        {
            if (isDisposed || viewport == null)
            {
                return;
            }

            if (Mathf.Abs(lastViewportWidth - viewport.rect.width) > 0.5f)
            {
                RebuildContentHeight();
                UpdateVisibleCells();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            DestroyAllCells();
        }

        private void OnScrollChanged(Vector2 _)
        {
            UpdateVisibleCells();
        }

        private void RebuildContentHeight()
        {
            lastViewportWidth = viewport.rect.width;
            columns = CalculateColumns(lastViewportWidth);

            int rowCount = Mathf.CeilToInt(items.Count / (float)columns);
            float totalHeight = padding.top + padding.bottom;
            if (rowCount > 0)
            {
                totalHeight += rowCount * cellSize.y + Mathf.Max(0, rowCount - 1) * spacing.y;
            }

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(totalHeight, viewport.rect.height));
        }

        private int CalculateColumns(float viewportWidth)
        {
            float availableWidth = Mathf.Max(1f, viewportWidth - padding.left - padding.right + spacing.x);
            return Mathf.Max(1, Mathf.FloorToInt(availableWidth / (cellSize.x + spacing.x)));
        }

        private void UpdateVisibleCells()
        {
            if (items.Count == 0)
            {
                RecycleAll();
                return;
            }

            RefreshColumnsIfNeeded();

            float rowHeight = cellSize.y + spacing.y;
            float top = Mathf.Max(0f, content.anchoredPosition.y);
            float bottom = top + viewport.rect.height;
            int rowCount = Mathf.CeilToInt(items.Count / (float)columns);
            int firstRow = Mathf.Clamp(Mathf.FloorToInt((top - padding.top) / rowHeight) - bufferRows, 0, rowCount - 1);
            int lastRow = Mathf.Clamp(Mathf.CeilToInt((bottom - padding.top) / rowHeight) + bufferRows, 0, rowCount - 1);
            int firstIndex = firstRow * columns;
            int lastIndex = Mathf.Min(items.Count - 1, (lastRow + 1) * columns - 1);

            itemsToRecycle.Clear();
            foreach (int itemIndex in visibleCellsByIndex.Keys)
            {
                if (itemIndex < firstIndex || itemIndex > lastIndex)
                {
                    itemsToRecycle.Add(itemIndex);
                }
            }

            for (int index = 0; index < itemsToRecycle.Count; index++)
            {
                Recycle(itemsToRecycle[index]);
            }

            for (int itemIndex = firstIndex; itemIndex <= lastIndex; itemIndex++)
            {
                if (visibleCellsByIndex.ContainsKey(itemIndex))
                {
                    continue;
                }

                TCell cell = GetCell();
                visibleCellsByIndex[itemIndex] = cell;
                PlaceCell(cell.RectTransform, itemIndex);
                cell.Bind(items[itemIndex], itemIndex);
                ItemBecameVisible?.Invoke(itemIndex);
            }
        }

        private void RefreshColumnsIfNeeded()
        {
            int nextColumns = CalculateColumns(viewport.rect.width);
            if (nextColumns == columns)
            {
                return;
            }

            RecycleAll();
            RebuildContentHeight();
        }

        private TCell GetCell()
        {
            TCell cell = pool.Count > 0 ? pool.Pop() : createCell.Invoke();
            cell.RectTransform.gameObject.SetActive(true);
            return cell;
        }

        private void Recycle(int itemIndex)
        {
            if (!visibleCellsByIndex.TryGetValue(itemIndex, out TCell cell))
            {
                return;
            }

            visibleCellsByIndex.Remove(itemIndex);
            cell.Unbind();
            cell.RectTransform.gameObject.SetActive(false);
            pool.Push(cell);
        }

        private void RecycleAll()
        {
            itemsToRecycle.Clear();
            foreach (int itemIndex in visibleCellsByIndex.Keys)
            {
                itemsToRecycle.Add(itemIndex);
            }

            for (int index = 0; index < itemsToRecycle.Count; index++)
            {
                Recycle(itemsToRecycle[index]);
            }
        }

        private void DestroyAllCells()
        {
            foreach (TCell cell in visibleCellsByIndex.Values)
            {
                cell.Unbind();
                UnityEngine.Object.Destroy(cell.RectTransform.gameObject);
            }

            visibleCellsByIndex.Clear();

            while (pool.Count > 0)
            {
                TCell cell = pool.Pop();
                cell.Unbind();
                UnityEngine.Object.Destroy(cell.RectTransform.gameObject);
            }

            items.Clear();
            itemsToRecycle.Clear();
        }

        private void PlaceCell(RectTransform rectTransform, int itemIndex)
        {
            int row = itemIndex / columns;
            int column = itemIndex % columns;
            float x = padding.left + column * (cellSize.x + spacing.x);
            float y = -padding.top - row * (cellSize.y + spacing.y);

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cellSize.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cellSize.y);
        }
    }
}

