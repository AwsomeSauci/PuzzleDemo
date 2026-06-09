using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PuzzleFlow.Presentation.Navigation
{
    public sealed class ReusableFragmentRouter : IFragmentRouter
    {
        private readonly IFragmentFactory fragmentFactory;
        private readonly Stack<FragmentHandle> overlayStack = new Stack<FragmentHandle>();
        private readonly Dictionary<string, FragmentHandle> activeHandlesById = new Dictionary<string, FragmentHandle>();
        private readonly SynchronizationContext synchronizationContext;
        private readonly Transform screenRoot;
        private readonly Transform overlayRoot;

        private FragmentHandle activeScreen;
        private int overlaySortingOrder;

        public FragmentId FocusFragmentId { get; private set; } = FragmentId.Empty;
        public event System.Action<FragmentId> FocusFragmentChanged;

        public ReusableFragmentRouter(IFragmentFactory fragmentFactory, Transform root = null)
        {
            this.fragmentFactory = fragmentFactory ?? throw new System.ArgumentNullException(nameof(fragmentFactory));
            synchronizationContext = SynchronizationContext.Current;
            Transform fragmentsRoot = root != null ? root : CreateRoot("ReusableFragments");
            screenRoot = CreateRoot("Screens", fragmentsRoot);
            overlayRoot = CreateRoot("Overlays", fragmentsRoot);
        }

        public void PreloadAll()
        {
            if (fragmentFactory is IPreloadableFragmentFactory preloadableFactory)
            {
                preloadableFactory.PreloadAll(screenRoot, overlayRoot);
            }
        }

        public UniTask<TResult> ShowAsync<TArgs, TResult>(
            string fragmentId,
            TArgs args,
            CancellationToken cancellationToken = default)
        {
            return ShowAsync<TArgs, TResult>(new FragmentId(fragmentId), args, cancellationToken);
        }

        public UniTask<TResult> ShowAsync<TArgs, TResult>(
            FragmentId fragmentId,
            TArgs args,
            CancellationToken cancellationToken = default)
        {
            if (fragmentId.IsEmpty)
            {
                throw new System.ArgumentOutOfRangeException(nameof(fragmentId), fragmentId, "Fragment id cannot be empty.");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled<TResult>(cancellationToken);
            }

            string fragmentKey = fragmentId.Value;
            FragmentLayer layer = fragmentFactory.GetLayer(fragmentId);
            int sortingOrder = layer == FragmentLayer.Screen ? 100 : ++overlaySortingOrder + 200;
            Transform parent = layer == FragmentLayer.Screen ? screenRoot : overlayRoot;

            if (activeHandlesById.TryGetValue(fragmentKey, out FragmentHandle existingHandle) &&
                !existingHandle.IsCompleted)
            {
                existingHandle.Rebuild(args);
                SetFocus(fragmentId);
                return AwaitTypedResult<TResult>(existingHandle.CompletionTask, cancellationToken);
            }

            if (layer == FragmentLayer.Screen && activeScreen != null && activeScreen.FragmentKey != fragmentKey)
            {
                Complete(activeScreen, default);
            }

            if (activeHandlesById.TryGetValue(fragmentKey, out existingHandle))
            {
                Complete(existingHandle, default);
            }

            IRoutableFragment fragment = fragmentFactory.GetOrCreate(fragmentId, parent, sortingOrder);
            UniTaskCompletionSource<object> completion = new UniTaskCompletionSource<object>();
            FragmentHandle handle = new FragmentHandle(fragmentId, fragment, completion);
            activeHandlesById[fragmentKey] = handle;

            if (layer == FragmentLayer.Screen)
            {
                activeScreen = handle;
            }
            else
            {
                overlayStack.Push(handle);
            }

            RegisterCancellation(handle, cancellationToken);
            if (handle.IsCompleted)
            {
                return AwaitTypedResult<TResult>(completion.Task);
            }

            try
            {
                fragment.Open(args, new FragmentController(result => CompleteOnOwnerContext(handle, result)));
            }
            catch (System.Exception exception)
            {
                Fault(handle, exception);
            }

            if (!handle.IsCompleted)
            {
                SetFocus(fragmentId);
            }

            return AwaitTypedResult<TResult>(completion.Task);
        }

        public UniTask<TResult> ShowAsync<TArgs, TResult>(
            FragmentRoute<TArgs, TResult> route,
            TArgs args,
            CancellationToken cancellationToken = default)
        {
            return ShowAsync<TArgs, TResult>(route.FragmentId, args, cancellationToken);
        }

        public bool TryCloseTop()
        {
            while (overlayStack.Count > 0)
            {
                FragmentHandle handle = overlayStack.Peek();
                if (handle.IsCompleted)
                {
                    overlayStack.Pop();
                    continue;
                }

                Complete(handle, null);
                return true;
            }

            return false;
        }

        public bool TryClose(string fragmentId)
        {
            return TryClose(new FragmentId(fragmentId));
        }

        public bool TryClose(FragmentId fragmentId)
        {
            if (fragmentId.IsEmpty ||
                !activeHandlesById.TryGetValue(fragmentId.Value, out FragmentHandle handle) ||
                handle.IsCompleted)
            {
                return false;
            }

            Complete(handle, null);
            return true;
        }

        public bool TryClose<TArgs, TResult>(FragmentRoute<TArgs, TResult> route)
        {
            return TryClose(route.FragmentId);
        }

        public bool TryRebuild(string fragmentId, object args = null)
        {
            return TryRebuild(new FragmentId(fragmentId), args);
        }

        public bool TryRebuild(FragmentId fragmentId, object args = null)
        {
            if (fragmentId.IsEmpty ||
                !activeHandlesById.TryGetValue(fragmentId.Value, out FragmentHandle handle) ||
                handle.IsCompleted)
            {
                return false;
            }

            handle.Rebuild(args);
            SetFocus(fragmentId);
            return true;
        }

        public bool TryRebuild<TArgs, TResult>(FragmentRoute<TArgs, TResult> route, TArgs args = default)
        {
            return TryRebuild(route.FragmentId, args);
        }

        public bool IsActive(string fragmentId)
        {
            return IsActive(new FragmentId(fragmentId));
        }

        public bool IsActive(FragmentId fragmentId)
        {
            return !fragmentId.IsEmpty &&
                   activeHandlesById.TryGetValue(fragmentId.Value, out FragmentHandle handle) &&
                   !handle.IsCompleted;
        }

        public bool IsActive<TArgs, TResult>(FragmentRoute<TArgs, TResult> route)
        {
            return IsActive(route.FragmentId);
        }

        private static async UniTask<TResult> AwaitTypedResult<TResult>(UniTask<object> task)
        {
            object result = await task;
            return result == null ? default : (TResult)result;
        }

        private static async UniTask<TResult> AwaitTypedResult<TResult>(
            UniTask<object> task,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new System.OperationCanceledException(cancellationToken);
            }

            object result = cancellationToken.CanBeCanceled
                ? await task.AttachExternalCancellation(cancellationToken)
                : await task;
            return result == null ? default : (TResult)result;
        }

        private void CompleteOnOwnerContext(FragmentHandle handle, object result)
        {
            if (synchronizationContext != null && SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext.Post(_ => Complete(handle, result), null);
                return;
            }

            Complete(handle, result);
        }

        private void Complete(FragmentHandle handle, object result)
        {
            if (handle == null || handle.IsCompleted)
            {
                return;
            }

            if (activeHandlesById.TryGetValue(handle.FragmentKey, out FragmentHandle activeHandle) && activeHandle == handle)
            {
                activeHandlesById.Remove(handle.FragmentKey);
            }

            if (activeScreen == handle)
            {
                activeScreen = null;
            }

            RemoveOverlayHandle(handle);
            handle.Complete(result);
            UpdateFocusAfterClose();
        }

        private void RemoveOverlayHandle(FragmentHandle handle)
        {
            if (overlayStack.Count == 0)
            {
                return;
            }

            if (overlayStack.Peek() == handle)
            {
                overlayStack.Pop();
                return;
            }

            Stack<FragmentHandle> kept = new Stack<FragmentHandle>();
            while (overlayStack.Count > 0)
            {
                FragmentHandle current = overlayStack.Pop();
                if (current != handle)
                {
                    kept.Push(current);
                }
            }

            while (kept.Count > 0)
            {
                overlayStack.Push(kept.Pop());
            }
        }

        private void CancelOnOwnerContext(FragmentHandle handle, CancellationToken cancellationToken)
        {
            if (synchronizationContext != null && SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext.Post(_ => Cancel(handle, cancellationToken), null);
                return;
            }

            Cancel(handle, cancellationToken);
        }

        private void Cancel(FragmentHandle handle, CancellationToken cancellationToken)
        {
            if (handle == null || handle.IsCompleted)
            {
                return;
            }

            if (activeHandlesById.TryGetValue(handle.FragmentKey, out FragmentHandle activeHandle) && activeHandle == handle)
            {
                activeHandlesById.Remove(handle.FragmentKey);
            }

            if (activeScreen == handle)
            {
                activeScreen = null;
            }

            RemoveOverlayHandle(handle);
            handle.Cancel(cancellationToken);
            UpdateFocusAfterClose();
        }

        private void Fault(FragmentHandle handle, System.Exception exception)
        {
            if (handle == null || handle.IsCompleted)
            {
                return;
            }

            if (activeHandlesById.TryGetValue(handle.FragmentKey, out FragmentHandle activeHandle) && activeHandle == handle)
            {
                activeHandlesById.Remove(handle.FragmentKey);
            }

            if (activeScreen == handle)
            {
                activeScreen = null;
            }

            RemoveOverlayHandle(handle);
            handle.Fault(exception);
            UpdateFocusAfterClose();
        }

        private void RegisterCancellation(
            FragmentHandle handle,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                handle.AddCancellationRegistration(
                    cancellationToken.Register(() => CancelOnOwnerContext(handle, cancellationToken)));
            }
        }

        private void SetFocus(FragmentId fragmentId)
        {
            if (FocusFragmentId == fragmentId)
            {
                return;
            }

            FocusFragmentId = fragmentId;
            FocusFragmentChanged?.Invoke(FocusFragmentId);
        }

        private void UpdateFocusAfterClose()
        {
            FragmentHandle focusedOverlay = TryPeekActiveOverlay();
            if (focusedOverlay != null)
            {
                SetFocus(focusedOverlay.FragmentId);
                return;
            }

            if (activeScreen != null && !activeScreen.IsCompleted)
            {
                SetFocus(activeScreen.FragmentId);
                return;
            }

            SetFocus(FragmentId.Empty);
        }

        private FragmentHandle TryPeekActiveOverlay()
        {
            while (overlayStack.Count > 0)
            {
                FragmentHandle handle = overlayStack.Peek();
                if (!handle.IsCompleted)
                {
                    return handle;
                }

                overlayStack.Pop();
            }

            return null;
        }

        private static Transform CreateRoot(string name, Transform parent = null)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rectTransform = (RectTransform)root.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return rectTransform;
        }

        private sealed class FragmentHandle
        {
            private readonly IRoutableFragment fragment;
            private readonly UniTaskCompletionSource<object> completion;
            private readonly List<CancellationTokenRegistration> cancellationRegistrations = new List<CancellationTokenRegistration>();

            public FragmentHandle(FragmentId fragmentId, IRoutableFragment fragment, UniTaskCompletionSource<object> completion)
            {
                FragmentId = fragmentId;
                this.fragment = fragment;
                this.completion = completion;
            }

            public FragmentId FragmentId { get; }
            public string FragmentKey => FragmentId.Value;
            public bool IsCompleted { get; private set; }
            public UniTask<object> CompletionTask => completion.Task;

            public void AddCancellationRegistration(CancellationTokenRegistration registration)
            {
                if (IsCompleted)
                {
                    registration.Dispose();
                    return;
                }

                cancellationRegistrations.Add(registration);
            }

            public void Rebuild(object args)
            {
                if (!IsCompleted)
                {
                    fragment.Rebuild(args);
                }
            }

            public void Complete(object result)
            {
                if (IsCompleted)
                {
                    return;
                }

                IsCompleted = true;
                Release();
                completion.TrySetResult(result);
            }

            public void Cancel(CancellationToken cancellationToken)
            {
                if (IsCompleted)
                {
                    return;
                }

                IsCompleted = true;
                Release();
                completion.TrySetCanceled(cancellationToken);
            }

            public void Fault(System.Exception exception)
            {
                if (IsCompleted)
                {
                    return;
                }

                IsCompleted = true;
                Release();
                completion.TrySetException(exception);
            }

            private void Release()
            {
                for (int index = 0; index < cancellationRegistrations.Count; index++)
                {
                    try
                    {
                        cancellationRegistrations[index].Dispose();
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                cancellationRegistrations.Clear();
                if (fragment is Fragment uiFragment && uiFragment != null)
                {
                    try
                    {
                        uiFragment.HideFragment();
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogException(exception, uiFragment);
                    }
                }
            }
        }

        private sealed class FragmentController : IFragmentController
        {
            private readonly System.Action<object> close;

            public FragmentController(System.Action<object> close)
            {
                this.close = close;
            }

            public void Close(object result)
            {
                close.Invoke(result);
            }
        }
    }
}

