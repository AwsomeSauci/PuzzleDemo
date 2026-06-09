using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PuzzleFlow.Presentation.Navigation
{
    public interface IFragmentRouter
    {
        FragmentId FocusFragmentId { get; }
        event Action<FragmentId> FocusFragmentChanged;

        UniTask<TResult> ShowAsync<TArgs, TResult>(
            string fragmentId,
            TArgs args,
            CancellationToken cancellationToken = default);

        UniTask<TResult> ShowAsync<TArgs, TResult>(
            FragmentId fragmentId,
            TArgs args,
            CancellationToken cancellationToken = default);

        UniTask<TResult> ShowAsync<TArgs, TResult>(
            FragmentRoute<TArgs, TResult> route,
            TArgs args,
            CancellationToken cancellationToken = default);

        bool TryCloseTop();
        bool TryClose(string fragmentId);
        bool TryClose(FragmentId fragmentId);
        bool TryClose<TArgs, TResult>(FragmentRoute<TArgs, TResult> route);
        bool TryRebuild(string fragmentId, object args = null);
        bool TryRebuild(FragmentId fragmentId, object args = null);
        bool TryRebuild<TArgs, TResult>(FragmentRoute<TArgs, TResult> route, TArgs args = default);
        bool IsActive(string fragmentId);
        bool IsActive(FragmentId fragmentId);
        bool IsActive<TArgs, TResult>(FragmentRoute<TArgs, TResult> route);
    }
}
