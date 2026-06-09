using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleFlow.Presentation.State;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzlePreviewFragmentView :
        MonoBehaviour,
        IPuzzlePreviewView,
        IEnumViewStateSource<PuzzleStartButtonState>
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private Image preview;
        [SerializeField] private TMP_Text progress;
        [SerializeField] private TMP_Text wallet;

        [SerializeField] private RectTransform pieceButtonsRoot;
        [SerializeField] private PieceCountButtonView pieceButtonPrefab;

        [SerializeField] private Button closeButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;

        [SerializeField] private string collectionNameSuffix;
        [SerializeField] private string walletPrefix;
        [SerializeField] private string walletSuffix;
        [SerializeField] private string progressPrefix;
        [SerializeField] private string progressMiddle;
        [SerializeField] private string progressSuffix;
        [SerializeField] private string noProgressText;

        private readonly List<PieceCountButtonView> activePieceButtons = new List<PieceCountButtonView>();
        private readonly Stack<PieceCountButtonView> pieceButtonPool = new Stack<PieceCountButtonView>();
        private PuzzleStartButtonViewModel currentStartButton = PuzzleStartButtonViewModel.None;
        private PuzzleStartButtonState lastStartButtonState = PuzzleStartButtonState.None;
        private PuzzleStartButtonState currentState = PuzzleStartButtonState.None;
        private bool isBusy;
        private bool isInitialized;

        public PuzzleStartButtonState CurrentState => currentState;
        public PuzzleStartButtonViewModel CurrentStartButton => currentStartButton;
        public event Action<PuzzleStartButtonState> StateChanged;
        public event Action<PuzzleStartButtonViewModel> StartButtonChanged;
        public event Action<int> PieceCountSelected;
        public event Action StartClicked;
        public event Action ContinueClicked;
        public event Action CloseClicked;

        public void Initialize()
        {
            isInitialized = true;
        }

        public void Render(PuzzlePreviewViewModel viewModel)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            title.text = viewModel.Puzzle.Title;
            subtitle.text = viewModel.Puzzle.CollectionName + collectionNameSuffix;
            preview.sprite = viewModel.Preview;
            wallet.text = walletPrefix + viewModel.WalletBalance + walletSuffix;

            RenderPieceButtons(viewModel.CutOptions);
            RenderProgress(viewModel);
            SetStartButtonState(viewModel.StartButton ?? PuzzleStartButtonViewModel.None);
            continueButton.gameObject.SetActive(viewModel.CanContinue);
        }

        public void SetBusy(bool isBusy)
        {
            this.isBusy = isBusy;
            startButton.interactable = !isBusy;
            continueButton.interactable = !isBusy;
            if (isBusy)
            {
                SetState(PuzzleStartButtonState.Busy);
                return;
            }

            SetState(lastStartButtonState);
        }

        public void Clear()
        {
            RecyclePieceButtons();

            if (preview != null)
            {
                preview.sprite = null;
            }

            isBusy = false;
            SetStartButtonState(PuzzleStartButtonViewModel.None);
            isInitialized = false;
        }

        public void OnStartClicked()
        {
            StartClicked?.Invoke();
        }

        public void OnContinueClicked()
        {
            ContinueClicked?.Invoke();
        }

        public void OnCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void OnDestroy()
        {
            Clear();
            DisposePieceButtonPool();
        }

        private void RenderPieceButtons(IReadOnlyList<PuzzleCutOptionViewModel> options)
        {
            RecyclePieceButtons();

            for (int index = 0; index < options.Count; index++)
            {
                PuzzleCutOptionViewModel option = options[index];
                PieceCountButtonView buttonView = GetPieceButton();
                buttonView.Bind(
                    option.PieceCount,
                    option.Label,
                    option.IsSelected,
                    OnPieceCountSelected);
                activePieceButtons.Add(buttonView);
            }
        }

        private PieceCountButtonView GetPieceButton()
        {
            PieceCountButtonView buttonView = pieceButtonPool.Count > 0
                ? pieceButtonPool.Pop()
                : Instantiate(pieceButtonPrefab, pieceButtonsRoot);

            buttonView.gameObject.SetActive(true);
            buttonView.transform.SetAsLastSibling();
            return buttonView;
        }

        private void RecyclePieceButtons()
        {
            for (int index = activePieceButtons.Count - 1; index >= 0; index--)
            {
                PieceCountButtonView buttonView = activePieceButtons[index];
                buttonView.Unbind();
                buttonView.gameObject.SetActive(false);
                pieceButtonPool.Push(buttonView);
            }

            activePieceButtons.Clear();
        }

        private void DisposePieceButtonPool()
        {
            while (pieceButtonPool.Count > 0)
            {
                PieceCountButtonView buttonView = pieceButtonPool.Pop();
                if (buttonView != null)
                {
                    Destroy(buttonView.gameObject);
                }
            }
        }

        private void RenderProgress(PuzzlePreviewViewModel viewModel)
        {
            if (viewModel.CanContinue)
            {
                progress.text = progressPrefix +
                                viewModel.Progress.CompletedPieces +
                                progressMiddle +
                                viewModel.Progress.PieceCount +
                                progressSuffix;
                progress.color = new Color(0.16f, 0.45f, 0.31f);
                return;
            }

            progress.text = noProgressText;
            progress.color = new Color(0.42f, 0.43f, 0.47f);
        }

        private void SetStartButtonState(PuzzleStartButtonViewModel viewModel)
        {
            currentStartButton = viewModel ?? PuzzleStartButtonViewModel.None;
            lastStartButtonState = currentStartButton.State;
            StartButtonChanged?.Invoke(currentStartButton);

            if (!isBusy)
            {
                SetState(currentStartButton.State);
            }
        }

        private void SetState(PuzzleStartButtonState state)
        {
            if (currentState == state)
            {
                return;
            }

            currentState = state;
            StateChanged?.Invoke(currentState);
        }

        private void OnPieceCountSelected(int pieceCount)
        {
            PieceCountSelected?.Invoke(pieceCount);
        }
    }
}
