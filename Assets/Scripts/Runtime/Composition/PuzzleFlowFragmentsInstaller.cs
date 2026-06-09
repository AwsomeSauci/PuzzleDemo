using System;
using PuzzleFlow.Presentation.Gallery;
using PuzzleFlow.Presentation.Navigation;
using PuzzleFlow.Presentation.Popups;
using PuzzleFlow.Presentation.PuzzlePreview;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Runtime
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Installers/Fragments", fileName = "PuzzleFlowFragmentsInstaller")]
    public sealed class PuzzleFlowFragmentsInstaller : ScriptableObjectInstaller<PuzzleFlowFragmentsInstaller>
    {
        [SerializeField] private FragmentRegistry fragmentRegistry;
        [SerializeField] private PopupRegistry popupRegistry;
        [SerializeField] private PuzzlePopupCatalog popupCatalog;
        [SerializeField] private bool showGalleryOnStart = true;

        [Header("Start Option Copy")]
        [SerializeField] private string freeCutLabel = PuzzleStartOptionPresentationTextSet.DefaultFreeCutLabel;
        [SerializeField] private string freeStartLabel = PuzzleStartOptionPresentationTextSet.DefaultFreeStartLabel;
        [SerializeField] private string coinCutFormat = PuzzleStartOptionPresentationTextSet.DefaultCoinCutFormat;
        [SerializeField] private string coinStartFormat = PuzzleStartOptionPresentationTextSet.DefaultCoinStartFormat;
        [SerializeField] private string coinCurrencyCode = PuzzleStartOptionPresentationTextSet.DefaultCoinCurrencyCode;
        [SerializeField] private string rewardedAdCutLabel = PuzzleStartOptionPresentationTextSet.DefaultRewardedAdCutLabel;
        [SerializeField] private string rewardedAdStartLabel = PuzzleStartOptionPresentationTextSet.DefaultRewardedAdStartLabel;

        public override void InstallBindings()
        {
            if (fragmentRegistry == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PuzzleFlowFragmentsInstaller)} requires {nameof(fragmentRegistry)}.");
            }

            if (popupCatalog == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PuzzleFlowFragmentsInstaller)} requires {nameof(popupCatalog)}.");
            }

            fragmentRegistry.ValidateOrThrow();
            popupCatalog.ValidateOrThrow();
            popupRegistry?.ValidateOrThrow();

            BindPopupCatalogs();
            BindStartOptionPresentation();
            BindFragments();
            BindPresenters();
            BindBootstrapper();
        }

        private void BindPopupCatalogs()
        {
            if (popupRegistry != null)
            {
                Container.BindInstance(popupRegistry).AsSingle();
            }

            Container.Bind<IPuzzlePopupCatalog>().FromInstance(popupCatalog).AsSingle();
        }

        private void BindStartOptionPresentation()
        {
            PuzzleStartOptionPresentationTextSet textSet = new PuzzleStartOptionPresentationTextSet(
                freeCutLabel,
                freeStartLabel,
                coinCutFormat,
                coinStartFormat,
                coinCurrencyCode,
                rewardedAdCutLabel,
                rewardedAdStartLabel);
            Container.BindInstance(textSet).AsSingle();

            Container.Bind<IStartOptionPresentationStrategy>()
                .To<FreeStartOptionPresentationStrategy>()
                .AsSingle();

            Container.Bind<IStartOptionPresentationStrategy>()
                .To<CoinStartOptionPresentationStrategy>()
                .AsSingle();

            Container.Bind<IStartOptionPresentationStrategy>()
                .To<RewardedAdStartOptionPresentationStrategy>()
                .AsSingle();

            Container.Bind<IPuzzleStartOptionViewModelFactory>()
                .FromMethod(context => new PuzzleStartOptionViewModelFactory(
                    context.Container.ResolveAll<IStartOptionPresentationStrategy>()))
                .AsSingle();
        }

        private void BindFragments()
        {
            Container.BindInstance(fragmentRegistry).AsSingle();

            Container.BindInterfacesAndSelfTo<ReusableFragmentFactory>()
                .AsSingle();

            Container.Bind<ReusableFragmentRouter>()
                .FromMethod(context => new ReusableFragmentRouter(
                    context.Container.Resolve<IFragmentFactory>()))
                .AsSingle()
                .NonLazy();

            Container.Bind<IFragmentRouter>()
                .To<ReusableFragmentRouter>()
                .FromResolve();

            Container.Bind<IUniversalPopupService>()
                .To<UniversalPopupService>()
                .AsSingle();
        }

        private void BindPresenters()
        {
            Container.Bind<GalleryPresenter>()
                .AsTransient();

            Container.Bind<PuzzlePreviewPresenterFactory>()
                .AsSingle();
        }

        private void BindBootstrapper()
        {
            Container.BindInterfacesTo<PuzzleFlowFragmentBootstrapper>()
                .AsSingle()
                .WithArguments(showGalleryOnStart)
                .NonLazy();
        }
    }
}
