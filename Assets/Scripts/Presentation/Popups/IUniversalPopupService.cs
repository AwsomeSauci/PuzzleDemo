using System.Threading;
using Cysharp.Threading.Tasks;

namespace PuzzleFlow.Presentation.Popups
{
    public interface IUniversalPopupService
    {
        UniTask<PopupResult> ShowAsync(string popupId, CancellationToken cancellationToken = default, params object[] args);
        UniTask<PopupResult> ShowAsync(PopupDefinition definition, CancellationToken cancellationToken = default, params object[] args);
        UniTask<PopupResult> ShowAsync(PopupRequest request, CancellationToken cancellationToken = default);
    }
}