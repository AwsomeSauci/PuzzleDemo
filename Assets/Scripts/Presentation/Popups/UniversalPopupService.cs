using PuzzleFlow.Presentation.Navigation;
using PuzzleFlow.Presentation;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PuzzleFlow.Presentation.Popups
{

    public sealed class UniversalPopupService : IUniversalPopupService
    {
        private readonly IFragmentRouter fragmentRouter;
        private readonly PopupRegistry popupRegistry;
        private readonly Queue<QueuedPopupRequest> requests = new Queue<QueuedPopupRequest>();
        private bool isProcessing;

        public UniversalPopupService(IFragmentRouter fragmentRouter, PopupRegistry popupRegistry = null)
        {
            this.fragmentRouter = fragmentRouter ?? throw new ArgumentNullException(nameof(fragmentRouter));
            this.popupRegistry = popupRegistry;
        }

        public UniTask<PopupResult> ShowAsync(
            string popupId,
            CancellationToken cancellationToken = default,
            params object[] args)
        {
            if (popupRegistry == null || !popupRegistry.TryGet(popupId, out PopupDefinition definition))
            {
                throw new KeyNotFoundException($"Popup '{popupId}' was not found.");
            }

            return ShowAsync(definition.BuildRequest(args), cancellationToken);
        }

        public UniTask<PopupResult> ShowAsync(
            PopupDefinition definition,
            CancellationToken cancellationToken = default,
            params object[] args)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return ShowAsync(definition.BuildRequest(args), cancellationToken);
        }

        public UniTask<PopupResult> ShowAsync(
            PopupRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Message == null)
            {
                throw new ArgumentException("Popup request has no message.", nameof(request));
            }

            if (request.Prefab == null)
            {
                throw new ArgumentException($"Popup request '{request.PopupId}' has no prefab.", nameof(request));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled<PopupResult>(cancellationToken);
            }

            QueuedPopupRequest queuedRequest = new QueuedPopupRequest(request, cancellationToken);
            requests.Enqueue(queuedRequest);
            StartProcessingIfNeeded();
            return queuedRequest.Task;
        }

        private void StartProcessingIfNeeded()
        {
            if (isProcessing)
            {
                return;
            }

            isProcessing = true;
            ProcessQueueAsync().Forget();
        }

        private async UniTask ProcessQueueAsync()
        {
            try
            {
                while (requests.Count > 0)
                {
                    QueuedPopupRequest queuedRequest = requests.Dequeue();
                    if (queuedRequest.IsCompleted)
                    {
                        queuedRequest.Dispose();
                        continue;
                    }

                    try
                    {
                        PopupResult result = await fragmentRouter.ShowAsync(
                            PuzzleFlowRoutes.UniversalPopup,
                            queuedRequest.Request,
                            queuedRequest.CancellationToken);
                        queuedRequest.TrySetResult(result ?? PopupResult.Dismissed(queuedRequest.Request.PopupId));
                    }
                    catch (OperationCanceledException)
                    {
                        queuedRequest.TrySetCanceled();
                    }
                    catch (Exception exception)
                    {
                        queuedRequest.TrySetException(exception);
                    }
                    finally
                    {
                        queuedRequest.Dispose();
                    }
                }
            }
            finally
            {
                isProcessing = false;
                if (requests.Count > 0)
                {
                    StartProcessingIfNeeded();
                }
            }
        }

        private sealed class QueuedPopupRequest : IDisposable
        {
            private readonly UniTaskCompletionSource<PopupResult> completion =
                new UniTaskCompletionSource<PopupResult>();
            private readonly CancellationTokenRegistration cancellationRegistration;
            private bool isCompleted;

            public QueuedPopupRequest(PopupRequest request, CancellationToken cancellationToken)
            {
                Request = request;
                CancellationToken = cancellationToken;
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationRegistration = cancellationToken.Register(() => TrySetCanceled());
                }
            }

            public PopupRequest Request { get; }
            public CancellationToken CancellationToken { get; }
            public UniTask<PopupResult> Task => completion.Task;
            public bool IsCompleted => isCompleted;

            public bool TrySetResult(PopupResult result)
            {
                bool completed = completion.TrySetResult(result);
                isCompleted |= completed;
                return completed;
            }

            public bool TrySetCanceled()
            {
                bool completed = CancellationToken.CanBeCanceled
                    ? completion.TrySetCanceled(CancellationToken)
                    : completion.TrySetCanceled();
                isCompleted |= completed;
                return completed;
            }

            public bool TrySetException(Exception exception)
            {
                bool completed = completion.TrySetException(exception);
                isCompleted |= completed;
                return completed;
            }

            public void Dispose()
            {
                cancellationRegistration.Dispose();
            }
        }
    }
}
