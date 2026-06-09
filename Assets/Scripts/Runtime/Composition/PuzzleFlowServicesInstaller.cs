using System;
using PuzzleFlow.Application;
using PuzzleFlow.Infrastructure;
using PuzzleFlow.Media;
using PuzzleFlow.Presentation.Media;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Runtime
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Installers/Services", fileName = "PuzzleFlowServicesInstaller")]
    public sealed class PuzzleFlowServicesInstaller : ScriptableObjectInstaller<PuzzleFlowServicesInstaller>
    {
        [SerializeField] private PuzzleCatalogAsset puzzleCatalog;
        [SerializeField] private PuzzleGallerySortConfigAsset gallerySortConfig;
        [SerializeField] private SerializedPuzzleProgressSeed[] progressSeeds;
        [SerializeField] private int demoStartingBalance = 300;
        [SerializeField] private DemoPurchaseMode demoPurchaseMode = DemoPurchaseMode.SucceedWhenAffordable;
        [SerializeField] private DemoRewardedAdMode demoRewardedAdMode = DemoRewardedAdMode.Completed;

        public override void InstallBindings()
        {
            if (puzzleCatalog == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PuzzleFlowServicesInstaller)} requires {nameof(puzzleCatalog)}.");
            }

            BindCatalog();
            BindDemoState();
            BindStartFlow();
            BindQueries();
            BindMedia();
        }

        private void BindCatalog()
        {
            ScriptablePuzzleCatalog catalogAdapter = new ScriptablePuzzleCatalog(puzzleCatalog);
            Container.Bind<IPuzzleCatalog>().FromInstance(catalogAdapter).AsSingle();
            Container.Bind<IPuzzleMediaCatalog>().FromInstance(catalogAdapter).AsSingle();

            IPuzzleGallerySortConfig sortConfig = gallerySortConfig != null
                ? gallerySortConfig
                : new DefaultPuzzleGallerySortConfig();
            Container.Bind<IPuzzleGallerySortConfig>().FromInstance(sortConfig).AsSingle();
        }

        private void BindDemoState()
        {
            Container.Bind<IPuzzleProgressRepository>()
                .FromInstance(new InMemoryPuzzleProgressRepository(
                    progressSeeds ?? Array.Empty<SerializedPuzzleProgressSeed>()))
                .AsSingle();

            Container.Bind<IPurchaseService>()
                .To<DemoPurchaseService>()
                .AsSingle()
                .WithArguments(demoStartingBalance, demoPurchaseMode);

            Container.Bind<IAdService>()
                .To<DemoAdService>()
                .AsSingle()
                .WithArguments(demoRewardedAdMode);
        }

        private void BindStartFlow()
        {
            Container.Bind<IStartOptionResolver>()
                .To<PuzzleCutStartOptionResolver>()
                .AsSingle();

            Container.Bind<IPuzzleStartModeHandler>()
                .To<FreePuzzleStartModeHandler>()
                .AsSingle();

            Container.Bind<IPuzzleStartModeHandler>()
                .To<CoinPuzzleStartModeHandler>()
                .AsSingle();

            Container.Bind<IPuzzleStartModeHandler>()
                .To<RewardedAdPuzzleStartModeHandler>()
                .AsSingle();

            Container.Bind<IPuzzleStartService>()
                .FromMethod(context => new PuzzleStartService(
                    context.Container.Resolve<IStartOptionResolver>(),
                    context.Container.ResolveAll<IPuzzleStartModeHandler>()))
                .AsSingle();
        }

        private void BindQueries()
        {
            Container.Bind<IPuzzleCatalogQueryService>()
                .To<PuzzleCatalogQueryService>()
                .AsSingle();

            Container.Bind<IPuzzleGalleryQueryService>()
                .To<PuzzleGalleryQueryService>()
                .AsSingle();

            Container.Bind<IPuzzleStartStateService>()
                .To<PuzzleStartStateService>()
                .AsSingle();

            Container.Bind<IPuzzleStartCommandService>()
                .To<PuzzleStartCommandService>()
                .AsSingle();

            Container.Bind<IPuzzleContinueService>()
                .To<PuzzleContinueService>()
                .AsSingle();

            Container.Bind<IPuzzleMediaQueryService>()
                .To<PuzzleMediaQueryService>()
                .AsSingle();
        }

        private void BindMedia()
        {
            Container.BindInterfacesAndSelfTo<AddressablesMediaSource>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<CachedMediaService>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<MediaSpriteService>()
                .AsSingle();
        }
    }
}
