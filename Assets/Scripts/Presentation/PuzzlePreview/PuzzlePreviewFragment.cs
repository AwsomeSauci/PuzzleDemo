using PuzzleFlow.Presentation.Navigation;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzlePreviewFragment : FragmentBase<PuzzlePreviewArgs, PuzzlePreviewResult>
    {
        [SerializeField] private PuzzlePreviewFragmentView view;

        private PuzzlePreviewPresenter presenter;
        private PuzzlePreviewArgs currentArgs;

        public IPuzzlePreviewView View => view;

        [Inject]
        public void Construct(PuzzlePreviewPresenterFactory presenterFactory)
        {
            presenter = presenterFactory.Create(view, CompleteFromPresenter);
        }

        public void CompleteFromPresenter(PuzzlePreviewResult result)
        {
            Close(result);
        }

        protected override void OnOpen(PuzzlePreviewArgs args)
        {
            currentArgs = args;
        }

        protected override void OnRefresh(PuzzlePreviewArgs args)
        {
            currentArgs = args;
            StartPresenter();
        }

        protected override void OnAppeared()
        {
            base.OnAppeared();
            StartPresenter();
        }

        protected override void OnClose()
        {
            presenter?.Stop();
            view.Clear();
        }

        private void StartPresenter()
        {
            view.Initialize();
            presenter.Start(currentArgs);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
